using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Domain.Enums;
using ShoppingList.Infrastructure.Persistence;
using ShoppingListEntity = ShoppingList.Domain.Entities.ShoppingList;

namespace ShoppingList.Api.Services;

public class ListAccessService(ApplicationDbContext db)
{
    public IQueryable<ShoppingListEntity> AccessibleLists(Guid userId) =>
        db.ShoppingLists.Where(l =>
            l.OwnerId == userId
            || l.SharedPermissions.Any(p =>
                p.UserId == userId && p.Status == ListShareStatus.Accepted));

    public async Task<ShoppingListEntity?> GetAccessibleListAsync(
        Guid listId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await AccessibleLists(userId)
            .Include(l => l.Items)
            .Include(l => l.Owner)
            .Include(l => l.SharedPermissions)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

    public async Task<ShoppingListEntity?> GetAccessibleListMetadataAsync(
        Guid listId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await AccessibleLists(userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

    public static ListSummaryDto ToSummary(ShoppingListEntity list, Guid userId)
    {
        var acceptedShareCount = list.SharedPermissions.Count(p => p.Status == ListShareStatus.Accepted);
        var ownerDisplayName = list.Owner?.PreferredName ?? list.Owner?.FirstName ?? string.Empty;

        return new ListSummaryDto(
            list.Id,
            list.Name,
            list.IsArchived,
            list.OwnerId == userId,
            list.UpdatedAt ?? list.CreatedAt,
            list.Items.Count,
            list.Items.Count(i => i.IsChecked),
            acceptedShareCount,
            ownerDisplayName);
    }

    public static ActionResult? RequireEditable(ShoppingListEntity list)
    {
        if (list.IsArchived)
        {
            return new BadRequestObjectResult(new
            {
                error = "This list is archived. Only the name can be changed."
            });
        }

        return null;
    }
}
