using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Domain.Entities;
using ShoppingList.Domain.Enums;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class RecipeSharingService(ApplicationDbContext db)
{
    public async Task<ShareRecipeResponse> ShareWithFriendsAsync(
        Guid recipeId,
        Guid ownerId,
        IReadOnlyList<Guid> friendUserIds,
        CancellationToken cancellationToken = default)
    {
        var recipe = await db.Recipes
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);

        if (recipe is null)
        {
            throw new InvalidOperationException("Recipe not found.");
        }

        if (recipe.OwnerId != ownerId)
        {
            throw new InvalidOperationException("Only the recipe owner can share this recipe.");
        }

        var uniqueFriendIds = friendUserIds
            .Where(id => id != Guid.Empty && id != ownerId)
            .Distinct()
            .ToList();

        if (uniqueFriendIds.Count == 0)
        {
            throw new InvalidOperationException("Select at least one friend to share with.");
        }

        var acceptedFriendIds = await GetAcceptedFriendIdsAsync(ownerId, cancellationToken);
        var invalidFriends = uniqueFriendIds.Where(id => !acceptedFriendIds.Contains(id)).ToList();
        if (invalidFriends.Count > 0)
        {
            throw new InvalidOperationException("You can only share with accepted friends.");
        }

        var existing = await db.RecipeSharedPermissions
            .Where(p => p.RecipeId == recipeId && uniqueFriendIds.Contains(p.UserId))
            .ToListAsync(cancellationToken);

        var invited = 0;
        var skipped = 0;

        foreach (var friendId in uniqueFriendIds)
        {
            var permission = existing.FirstOrDefault(p => p.UserId == friendId);
            if (permission is null)
            {
                db.RecipeSharedPermissions.Add(new RecipeSharedPermission
                {
                    Id = Guid.NewGuid(),
                    UserId = friendId,
                    RecipeId = recipeId,
                    PermissionLevel = PermissionLevel.Edit,
                    Status = ListShareStatus.Pending,
                    InvitedByUserId = ownerId,
                    CreatedAt = DateTime.UtcNow,
                });
                invited++;
                continue;
            }

            if (permission.Status == ListShareStatus.Declined)
            {
                permission.Status = ListShareStatus.Pending;
                permission.InvitedByUserId = ownerId;
                permission.GrantedAt = null;
                permission.UpdatedAt = DateTime.UtcNow;
                invited++;
            }
            else
            {
                skipped++;
            }
        }

        if (invited == 0)
        {
            return new ShareRecipeResponse(0, skipped, "Selected friends already have a pending or active share.");
        }

        recipe.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new ShareRecipeResponse(
            invited,
            skipped,
            invited == 1
                ? "Shared the recipe with 1 friend."
                : $"Shared the recipe with {invited} friends.");
    }

    public async Task<IReadOnlyList<PendingRecipeShareDto>> GetPendingInvitationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var permissions = await db.RecipeSharedPermissions
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.Status == ListShareStatus.Pending)
            .Include(p => p.Recipe)
                .ThenInclude(r => r.Ingredients)
            .Include(p => p.Recipe)
                .ThenInclude(r => r.Owner)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return permissions
            .Select(p => new PendingRecipeShareDto(
                p.Id,
                p.RecipeId,
                p.Recipe.Name,
                p.Recipe.Owner.PreferredName ?? p.Recipe.Owner.FirstName,
                p.InvitedByUserId,
                p.Recipe.Ingredients.Count))
            .ToList();
    }

    public async Task<RecipeSummaryDto> AcceptShareAsync(
        Guid permissionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var permission = await db.RecipeSharedPermissions
            .Include(p => p.Recipe)
                .ThenInclude(r => r.Ingredients)
            .FirstOrDefaultAsync(
                p => p.Id == permissionId && p.UserId == userId,
                cancellationToken);

        if (permission is null)
        {
            throw new InvalidOperationException("Share invitation not found.");
        }

        if (permission.Status != ListShareStatus.Pending)
        {
            throw new InvalidOperationException("This share invitation is no longer pending.");
        }

        permission.Status = ListShareStatus.Accepted;
        permission.GrantedAt = DateTime.UtcNow;
        permission.UpdatedAt = DateTime.UtcNow;
        permission.Recipe.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return RecipeAccessService.ToSummary(permission.Recipe, userId);
    }

    public async Task DeclineShareAsync(
        Guid permissionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var permission = await db.RecipeSharedPermissions
            .FirstOrDefaultAsync(
                p => p.Id == permissionId && p.UserId == userId,
                cancellationToken);

        if (permission is null)
        {
            throw new InvalidOperationException("Share invitation not found.");
        }

        if (permission.Status != ListShareStatus.Pending)
        {
            throw new InvalidOperationException("This share invitation is no longer pending.");
        }

        permission.Status = ListShareStatus.Declined;
        permission.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<HashSet<Guid>> GetAcceptedFriendIdsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var friendships = await db.Friendships
            .AsNoTracking()
            .Where(f =>
                f.Status == FriendshipStatus.Accepted
                && (f.RequesterId == userId || f.AddresseeId == userId))
            .Select(f => new { f.RequesterId, f.AddresseeId })
            .ToListAsync(cancellationToken);

        return friendships
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .ToHashSet();
    }
}
