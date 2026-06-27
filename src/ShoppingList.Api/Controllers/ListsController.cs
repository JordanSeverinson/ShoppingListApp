using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Api.Hubs;
using ShoppingList.Api.Services;
using ShoppingList.Application.Hubs;
using ShoppingList.Application.Parsing;
using ShoppingList.Domain.Entities;
using ShoppingList.Domain.Enums;
using ShoppingList.Infrastructure.Persistence;
using ShoppingListEntity = ShoppingList.Domain.Entities.ShoppingList;

namespace ShoppingList.Api.Controllers;

[ApiController]
[Route("api/lists")]
[AllowAnonymous]
public class ListsController(
    ApplicationDbContext db,
    IIngredientParserService ingredientParser,
    IHubContext<ShoppingListHub> hubContext,
    CurrentUserService currentUser,
    ListAccessService listAccess,
    ShareCodeAllocationService shareCodes) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ListSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListSummaryResponse>> GetMyLists(CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var lists = await listAccess.AccessibleLists(userId)
            .AsNoTracking()
            .Include(l => l.Items)
            .OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
            .ToListAsync(cancellationToken);

        var shared = lists
            .Where(l => !l.IsArchived)
            .Select(l => ListAccessService.ToSummary(l, userId))
            .ToList();

        var archived = lists
            .Where(l => l.IsArchived)
            .Select(l => ListAccessService.ToSummary(l, userId))
            .ToList();

        return Ok(new ListSummaryResponse(shared, archived));
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
            ShareCode = await shareCodes.AllocateUniqueShareCodeAsync(cancellationToken),
            CreatedAt = DateTime.UtcNow
        };

        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetList), new { listId = list.Id }, ListAccessService.ToSummary(list, userId));
    }

    [HttpPost("join")]
    [ProducesResponseType(typeof(ListSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListSummaryDto>> JoinList(
        [FromBody] JoinListRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var code = request.ShareCode?.Trim().ToUpperInvariant() ?? string.Empty;

        if (code.Length != 11)
        {
            return BadRequest(new { error = "Share code must be exactly 11 characters." });
        }

        var list = await db.ShoppingLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.ShareCode == code, cancellationToken);

        if (list is null)
        {
            return NotFound(new { error = "No list found for that share code." });
        }

        if (list.OwnerId == userId)
        {
            return Ok(ListAccessService.ToSummary(list, userId));
        }

        var alreadyJoined = await db.SharedPermissions
            .AnyAsync(p => p.UserId == userId && p.ShoppingListId == list.Id, cancellationToken);

        if (!alreadyJoined)
        {
            db.SharedPermissions.Add(new SharedPermission
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ShoppingListId = list.Id,
                PermissionLevel = PermissionLevel.Edit,
                GrantedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(ListAccessService.ToSummary(list, userId));
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
            return NotFound(new { error = $"Shopping list {listId} was not found." });
        }

        var items = list.Items
            .OrderBy(i => i.Category)
            .ThenBy(i => i.SortOrder)
            .Select(ToEventDto)
            .ToList();

        return Ok(new ListDetailResponse(
            list.Id,
            list.Name,
            list.ShareCode,
            list.IsArchived,
            CanEdit: !list.IsArchived,
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
            return NotFound(new { error = "List not found." });
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
        var list = await listAccess.GetAccessibleListAsync(listId, userId, cancellationToken);

        if (list is null)
        {
            return NotFound(new { error = "List not found." });
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

    [HttpDelete("{listId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteItem(
        Guid listId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
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

        db.ListItems.Remove(item);
        list.Value!.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await ShoppingListHub.ItemDeleted(hubContext, listId, itemId);

        return NoContent();
    }

    [HttpPost("{listId:guid}/upload-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadImageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadImageResponse>> UploadImage(
        Guid listId,
        IFormFile? image,
        CancellationToken cancellationToken)
    {
        var listResult = await LoadEditableListAsync(listId, cancellationToken);
        if (listResult.Result is not null)
        {
            return listResult.Result;
        }

        if (image is null || image.Length == 0)
        {
            return BadRequest(new { error = "An image file is required (form field: image)." });
        }

        if (!AllowedContentTypes.Contains(image.ContentType))
        {
            return BadRequest(new { error = $"Unsupported content type: {image.ContentType}" });
        }

        IReadOnlyList<ParsedIngredientDto> parsed;
        try
        {
            await using var stream = image.OpenReadStream();
            parsed = await ingredientParser.ParseFromStreamAsync(stream, cancellationToken);
        }
        catch (DirectoryNotFoundException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }

        if (parsed.Count == 0)
        {
            return Ok(new UploadImageResponse(listId, [], "No ingredients could be extracted from the image."));
        }

        var nextSortOrder = await db.ListItems
            .Where(i => i.ShoppingListId == listId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var now = DateTime.UtcNow;
        var entities = new List<ListItem>();

        foreach (var ingredient in parsed)
        {
            nextSortOrder++;
            entities.Add(new ListItem
            {
                Id = Guid.NewGuid(),
                ShoppingListId = listId,
                Name = ingredient.Name,
                Quantity = string.IsNullOrWhiteSpace(ingredient.Quantity) ? null : ingredient.Quantity,
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

        return Ok(new UploadImageResponse(
            listId,
            eventDtos,
            $"Added {eventDtos.Count} ingredient(s) from image."));
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

        var editableCheck = ListAccessService.RequireEditable(list);
        if (editableCheck is not null)
        {
            return editableCheck;
        }

        return list;
    }

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/bmp", "image/tiff"
    };

    private static ListItemEventDto ToEventDto(ListItem item) =>
        new(item.Id, item.ShoppingListId, item.Name, item.Quantity, item.Category, item.IsChecked, item.SortOrder);
}

public record UploadImageResponse(
    Guid ListId,
    IReadOnlyList<ListItemEventDto> Items,
    string Message);
