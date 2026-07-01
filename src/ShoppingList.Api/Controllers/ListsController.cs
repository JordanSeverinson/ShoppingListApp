using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Api.Hubs;
using ShoppingList.Api.Security;
using ShoppingList.Api.Services;
using ShoppingList.Application.Hubs;
using ShoppingList.Domain.Entities;
using ShoppingList.Domain.Enums;
using ShoppingList.Infrastructure.Persistence;
using ShoppingListEntity = ShoppingList.Domain.Entities.ShoppingList;

namespace ShoppingList.Api.Controllers;

[ApiController]
[Route("api/lists")]
[Authorize]
public class ListsController(
    ApplicationDbContext db,
    IHubContext<ShoppingListHub> hubContext,
    CurrentUserService currentUser,
    ListAccessService listAccess,
    ListSharingService listSharing,
    ListHubNotifier listHubNotifier,
    RecipeAccessService recipeAccess,
    ILogger<ListsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ListSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListSummaryResponse>> GetMyLists(CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var lists = await listAccess.AccessibleLists(userId)
            .AsNoTracking()
            .Include(l => l.Items)
            .Include(l => l.Owner)
            .Include(l => l.SharedPermissions)
            .OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
            .ToListAsync(cancellationToken);

        var active = lists
            .Where(l => !l.IsArchived)
            .Select(l => ListAccessService.ToSummary(l, userId))
            .ToList();

        var archived = lists
            .Where(l => l.IsArchived)
            .Select(l => ListAccessService.ToSummary(l, userId))
            .ToList();

        var pendingShares = await listSharing.GetPendingInvitationsAsync(userId, cancellationToken);

        return Ok(new ListSummaryResponse(active, archived, pendingShares));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ListSummaryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ListSummaryDto>> CreateList(
        [FromBody] CreateListRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var name = string.IsNullOrWhiteSpace(request.Name) ? "New grocery list" : request.Name.Trim();

        var list = new ShoppingListEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetList), new { listId = list.Id }, ListAccessService.ToSummary(list, userId));
    }

    [HttpPost("{listId:guid}/shares")]
    [ProducesResponseType(typeof(ShareListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShareListResponse>> ShareList(
        Guid listId,
        [FromBody] ShareListRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var result = await listSharing.ShareWithFriendsAsync(
                listId,
                userId,
                request.FriendUserIds ?? [],
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Share list failed for {ListId}", listId);
            return BadRequest(new { error = ApiErrors.ShareFailed });
        }
    }

    [HttpPost("shares/{permissionId:guid}/accept")]
    [ProducesResponseType(typeof(ListSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListSummaryDto>> AcceptShare(
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var summary = await listSharing.AcceptShareAsync(permissionId, userId, cancellationToken);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Accept list share failed for {PermissionId}", permissionId);
            return BadRequest(new { error = ApiErrors.ShareFailed });
        }
    }

    [HttpPost("shares/{permissionId:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeclineShare(
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await listSharing.DeclineShareAsync(permissionId, userId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Decline list share failed for {PermissionId}", permissionId);
            return BadRequest(new { error = ApiErrors.ShareFailed });
        }
    }

    [HttpPost("{listId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LeaveList(
        Guid listId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await listSharing.LeaveListAsync(listId, userId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Leave list failed for {ListId}", listId);
            return BadRequest(new { error = ApiErrors.ShareFailed });
        }
    }

    [HttpGet("{listId:guid}")]
    [ProducesResponseType(typeof(ListDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListDetailResponse>> GetList(Guid listId, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var list = await listAccess.GetAccessibleListAsync(listId, userId, cancellationToken);

        if (list is null)
        {
            return NotFound(new { error = ApiErrors.ListNotFound });
        }

        var items = list.Items
            .OrderBy(i => i.Category)
            .ThenBy(i => i.SortOrder)
            .Select(ToEventDto)
            .ToList();

        return Ok(new ListDetailResponse(
            list.Id,
            list.Name,
            list.IsArchived,
            list.OwnerId == userId,
            CanEdit: ListAccessService.CanEdit(list, userId) && !list.IsArchived,
            items));
    }

    [HttpPatch("{listId:guid}")]
    [ProducesResponseType(typeof(ListSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListSummaryDto>> RenameList(
        Guid listId,
        [FromBody] RenameListRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "List name is required." });
        }

        var userId = currentUser.GetUserId();
        var list = await listAccess.GetAccessibleListAsync(listId, userId, cancellationToken);

        if (list is null)
        {
            return NotFound(new { error = ApiErrors.ListNotFound });
        }

        var renameCheck = ListAccessService.RequireRenamable(list, userId);
        if (renameCheck is not null)
        {
            return renameCheck;
        }

        list.Name = request.Name.Trim();
        list.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ListAccessService.ToSummary(list, userId));
    }

    [HttpPost("{listId:guid}/archive")]
    [ProducesResponseType(typeof(ListSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListSummaryDto>> ArchiveList(
        Guid listId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var list = await db.ShoppingLists
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (list is null || list.OwnerId != userId)
        {
            return NotFound(new { error = ApiErrors.ListNotFound });
        }

        if (list.IsArchived)
        {
            return BadRequest(new { error = "List is already archived." });
        }

        list.IsArchived = true;
        list.ArchivedAt = DateTime.UtcNow;
        list.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ListAccessService.ToSummary(list, userId));
    }

    [HttpDelete("{listId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteList(Guid listId, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var list = await db.ShoppingLists
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (list is null || list.OwnerId != userId)
        {
            return NotFound(new { error = ApiErrors.ListNotFound });
        }

        db.ShoppingLists.Remove(list);
        await db.SaveChangesAsync(cancellationToken);

        await listHubNotifier.EvictAllFromListAsync(listId, cancellationToken);

        return NoContent();
    }

    [HttpPost("{listId:guid}/items")]
    [ProducesResponseType(typeof(ListItemEventDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ListItemEventDto>> CreateItem(
        Guid listId,
        [FromBody] CreateItemRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Item name is required." });
        }

        var list = await LoadEditableListAsync(listId, cancellationToken);
        if (list.Result is not null)
        {
            return list.Result;
        }

        var entity = list.Value!;

        var nextSortOrder = await db.ListItems
            .Where(i => i.ShoppingListId == listId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var item = new ListItem
        {
            Id = Guid.NewGuid(),
            ShoppingListId = listId,
            Name = request.Name.Trim(),
            Quantity = string.IsNullOrWhiteSpace(request.Quantity) ? null : request.Quantity.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Other" : request.Category.Trim(),
            IsChecked = false,
            SortOrder = nextSortOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        db.ListItems.Add(item);
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var dto = ToEventDto(item);
        await ShoppingListHub.ItemAdded(hubContext, listId, dto);

        return CreatedAtAction(nameof(GetList), new { listId }, dto);
    }

    [HttpPatch("{listId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(ListItemEventDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListItemEventDto>> UpdateItem(
        Guid listId,
        Guid itemId,
        [FromBody] UpdateItemRequest request,
        CancellationToken cancellationToken)
    {
        if (IsToggleOnly(request))
        {
            return await ToggleItemAsync(listId, itemId, request.IsChecked!.Value, cancellationToken);
        }

        var list = await LoadEditableListAsync(listId, cancellationToken);
        if (list.Result is not null)
        {
            return list.Result;
        }

        var item = await db.ListItems
            .FirstOrDefaultAsync(i => i.ShoppingListId == listId && i.Id == itemId, cancellationToken);

        if (item is null)
        {
            return NotFound(new { error = "Item not found." });
        }

        var toggled = false;

        if (request.Name is not null)
        {
            item.Name = request.Name.Trim();
        }

        if (request.Quantity is not null)
        {
            item.Quantity = string.IsNullOrWhiteSpace(request.Quantity) ? null : request.Quantity.Trim();
        }

        if (request.Category is not null)
        {
            item.Category = request.Category.Trim();
        }

        if (request.IsChecked is { } isChecked && item.IsChecked != isChecked)
        {
            item.IsChecked = isChecked;
            toggled = true;
        }

        item.UpdatedAt = DateTime.UtcNow;
        list.Value!.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var dto = ToEventDto(item);

        if (toggled)
        {
            await ShoppingListHub.ItemToggled(hubContext, listId, itemId, item.IsChecked);
        }
        else
        {
            await ShoppingListHub.ItemUpdated(hubContext, listId, dto);
        }

        return Ok(dto);
    }

    [HttpPost("{listId:guid}/items/check-all")]
    [ProducesResponseType(typeof(CheckAllItemsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckAllItemsResponse>> CheckAllItems(
        Guid listId,
        [FromBody] CheckAllItemsRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var list = await listAccess.GetAccessibleListMetadataAsync(listId, userId, cancellationToken);

        if (list is null)
        {
            return NotFound(new { error = "List not found." });
        }

        var editableCheck = ListAccessService.RequireEditable(list, userId);
        if (editableCheck is not null)
        {
            return editableCheck;
        }

        var category = string.IsNullOrWhiteSpace(request?.Category) ? null : request!.Category.Trim();
        var query = db.ListItems.Where(i => i.ShoppingListId == listId && !i.IsChecked);

        if (category is not null)
        {
            query = query.Where(i => i.Category == category);
        }

        var itemIds = await query.Select(i => i.Id).ToListAsync(cancellationToken);
        if (itemIds.Count == 0)
        {
            return Ok(new CheckAllItemsResponse(0, []));
        }

        var now = DateTime.UtcNow;

        await db.ListItems
            .Where(i => itemIds.Contains(i.Id))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(i => i.IsChecked, true)
                    .SetProperty(i => i.UpdatedAt, now),
                cancellationToken);

        await db.ShoppingLists
            .Where(l => l.Id == listId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(l => l.UpdatedAt, now),
                cancellationToken);

        await ShoppingListHub.ItemsBulkToggled(hubContext, listId, itemIds, true);

        return Ok(new CheckAllItemsResponse(itemIds.Count, itemIds));
    }

    [HttpDelete("{listId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteItem(
        Guid listId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var list = await listAccess.GetAccessibleListMetadataAsync(listId, userId, cancellationToken);

        if (list is null)
        {
            return NotFound(new { error = "List not found." });
        }

        var editableCheck = ListAccessService.RequireEditable(list, userId);
        if (editableCheck is not null)
        {
            return editableCheck;
        }

        var rowsDeleted = await db.ListItems
            .Where(i => i.ShoppingListId == listId && i.Id == itemId)
            .ExecuteDeleteAsync(cancellationToken);

        if (rowsDeleted == 0)
        {
            return NotFound(new { error = "Item not found." });
        }

        var now = DateTime.UtcNow;
        await db.ShoppingLists
            .Where(l => l.Id == listId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(l => l.UpdatedAt, now),
                cancellationToken);

        await ShoppingListHub.ItemDeleted(hubContext, listId, itemId);

        return NoContent();
    }

    [HttpPost("{listId:guid}/items/delete-many")]
    [ProducesResponseType(typeof(DeleteItemsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeleteItemsResponse>> DeleteItems(
        Guid listId,
        [FromBody] DeleteItemsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var list = await listAccess.GetAccessibleListMetadataAsync(listId, userId, cancellationToken);

        if (list is null)
        {
            return NotFound(new { error = "List not found." });
        }

        var editableCheck = ListAccessService.RequireEditable(list, userId);
        if (editableCheck is not null)
        {
            return editableCheck;
        }

        var requestedIds = (request.ItemIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (requestedIds.Count == 0)
        {
            return Ok(new DeleteItemsResponse(0, []));
        }

        if (requestedIds.Count > RequestLimits.MaxBulkOperationIds)
        {
            return BadRequest(new { error = $"At most {RequestLimits.MaxBulkOperationIds} items can be deleted per request." });
        }

        var itemIds = await db.ListItems
            .Where(i => i.ShoppingListId == listId && requestedIds.Contains(i.Id))
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        if (itemIds.Count == 0)
        {
            return NotFound(new { error = "No matching items found." });
        }

        await db.ListItems
            .Where(i => i.ShoppingListId == listId && itemIds.Contains(i.Id))
            .ExecuteDeleteAsync(cancellationToken);

        var now = DateTime.UtcNow;
        await db.ShoppingLists
            .Where(l => l.Id == listId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(l => l.UpdatedAt, now),
                cancellationToken);

        await ShoppingListHub.ItemsBulkDeleted(hubContext, listId, itemIds);

        return Ok(new DeleteItemsResponse(itemIds.Count, itemIds));
    }

    [HttpPost("{listId:guid}/import-recipe/{recipeId:guid}")]
    [ProducesResponseType(typeof(ImportRecipeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportRecipeResponse>> ImportRecipe(
        Guid listId,
        Guid recipeId,
        CancellationToken cancellationToken)
    {
        var listResult = await LoadEditableListAsync(listId, cancellationToken);
        if (listResult.Result is not null)
        {
            return listResult.Result;
        }

        var userId = currentUser.GetUserId();
        var recipe = await recipeAccess.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);

        if (recipe is null)
        {
            return NotFound(new { error = "Recipe not found." });
        }

        if (recipe.Ingredients.Count == 0)
        {
            return Ok(new ImportRecipeResponse(listId, [], "Recipe has no ingredients to add."));
        }

        var nextSortOrder = await db.ListItems
            .Where(i => i.ShoppingListId == listId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var now = DateTime.UtcNow;
        var entities = new List<ListItem>();

        foreach (var ingredient in recipe.Ingredients.OrderBy(i => i.Category).ThenBy(i => i.SortOrder))
        {
            nextSortOrder++;
            entities.Add(new ListItem
            {
                Id = Guid.NewGuid(),
                ShoppingListId = listId,
                Name = ingredient.Name,
                Quantity = ingredient.Quantity,
                Category = ingredient.Category,
                IsChecked = false,
                SortOrder = nextSortOrder,
                CreatedAt = now
            });
        }

        db.ListItems.AddRange(entities);
        listResult.Value!.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        var eventDtos = entities.Select(ToEventDto).ToList();
        await ShoppingListHub.ItemsBulkAdded(hubContext, listId, eventDtos);

        return Ok(new ImportRecipeResponse(
            listId,
            eventDtos,
            $"Added {eventDtos.Count} ingredient(s) from {recipe.Name}."));
    }

    private async Task<ActionResult<ShoppingListEntity>> LoadEditableListAsync(
        Guid listId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var list = await listAccess.GetAccessibleListAsync(listId, userId, cancellationToken);

        if (list is null)
        {
            return NotFound(new { error = "List not found." });
        }

        var editableCheck = ListAccessService.RequireEditable(list, userId);
        if (editableCheck is not null)
        {
            return editableCheck;
        }

        return list;
    }

    private async Task<ActionResult<ListItemEventDto>> ToggleItemAsync(
        Guid listId,
        Guid itemId,
        bool isChecked,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var list = await listAccess.GetAccessibleListMetadataAsync(listId, userId, cancellationToken);

        if (list is null)
        {
            return NotFound(new { error = "List not found." });
        }

        var editableCheck = ListAccessService.RequireEditable(list, userId);
        if (editableCheck is not null)
        {
            return editableCheck;
        }

        var now = DateTime.UtcNow;
        var rowsUpdated = await db.ListItems
            .Where(i => i.ShoppingListId == listId && i.Id == itemId && i.IsChecked != isChecked)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(i => i.IsChecked, isChecked)
                    .SetProperty(i => i.UpdatedAt, now),
                cancellationToken);

        if (rowsUpdated == 0)
        {
            var existing = await db.ListItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ShoppingListId == listId && i.Id == itemId, cancellationToken);

            if (existing is null)
            {
                return NotFound(new { error = "Item not found." });
            }

            return Ok(ToEventDto(existing));
        }

        await db.ShoppingLists
            .Where(l => l.Id == listId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(l => l.UpdatedAt, now),
                cancellationToken);

        var item = await db.ListItems
            .AsNoTracking()
            .FirstAsync(i => i.ShoppingListId == listId && i.Id == itemId, cancellationToken);

        await ShoppingListHub.ItemToggled(hubContext, listId, itemId, isChecked);

        return Ok(ToEventDto(item));
    }

    private static bool IsToggleOnly(UpdateItemRequest request) =>
        request.IsChecked is not null
        && request.Name is null
        && request.Quantity is null
        && request.Category is null;

    private static ListItemEventDto ToEventDto(ListItem item) =>
        new(item.Id, item.ShoppingListId, item.Name, item.Quantity, item.Category, item.IsChecked, item.SortOrder);
}
