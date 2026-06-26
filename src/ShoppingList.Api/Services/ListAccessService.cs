using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Infrastructure.Persistence;
using ShoppingListEntity = ShoppingList.Domain.Entities.ShoppingList;

namespace ShoppingList.Api.Services;

public class ListAccessService(ApplicationDbContext db)
{
    public IQueryable<ShoppingListEntity> AccessibleLists(Guid userId) =>
        db.ShoppingLists.Where(l =>
            l.OwnerId == userId
            || l.SharedPermissions.Any(p => p.UserId == userId));

    public async Task<ShoppingListEntity?> GetAccessibleListAsync(
        Guid listId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await AccessibleLists(userId)
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

    public static ListSummaryDto ToSummary(ShoppingListEntity list, Guid userId) =>
        new(
            list.Id,
            list.Name,
            list.ShareCode,
            list.IsArchived,
            list.OwnerId == userId,
            list.UpdatedAt ?? list.CreatedAt,
            list.Items.Count,
            list.Items.Count(i => i.IsChecked));

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
