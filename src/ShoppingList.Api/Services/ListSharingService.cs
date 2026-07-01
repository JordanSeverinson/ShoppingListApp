using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Domain.Entities;
using ShoppingList.Domain.Enums;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class ListSharingService(ApplicationDbContext db, ListHubNotifier listHubNotifier)
{
    public async Task<ShareListResponse> ShareWithFriendsAsync(
        Guid listId,
        Guid ownerId,
        IReadOnlyList<Guid> friendUserIds,
        CancellationToken cancellationToken = default)
    {
        var list = await db.ShoppingLists
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (list is null)
        {
            throw new InvalidOperationException("List not found.");
        }

        if (list.OwnerId != ownerId)
        {
            throw new InvalidOperationException("Only the list owner can share this list.");
        }

        if (list.IsArchived)
        {
            throw new InvalidOperationException("Archived lists cannot be shared.");
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

        var existing = await db.SharedPermissions
            .Where(p => p.ShoppingListId == listId && uniqueFriendIds.Contains(p.UserId))
            .ToListAsync(cancellationToken);

        var invited = 0;
        var skipped = 0;

        foreach (var friendId in uniqueFriendIds)
        {
            var permission = existing.FirstOrDefault(p => p.UserId == friendId);
            if (permission is null)
            {
                db.SharedPermissions.Add(new SharedPermission
                {
                    Id = Guid.NewGuid(),
                    UserId = friendId,
                    ShoppingListId = listId,
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
            return new ShareListResponse(0, skipped, "Selected friends already have a pending or active share.");
        }

        list.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new ShareListResponse(
            invited,
            skipped,
            invited == 1
                ? "Shared the list with 1 friend."
                : $"Shared the list with {invited} friends.");
    }

    public async Task<IReadOnlyList<PendingListShareDto>> GetPendingInvitationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var permissions = await db.SharedPermissions
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.Status == ListShareStatus.Pending)
            .Include(p => p.ShoppingList)
                .ThenInclude(l => l.Items)
            .Include(p => p.ShoppingList)
                .ThenInclude(l => l.Owner)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return permissions
            .Select(p => new PendingListShareDto(
                p.Id,
                p.ShoppingListId,
                p.ShoppingList.Name,
                p.ShoppingList.Owner.PreferredName ?? p.ShoppingList.Owner.FirstName,
                p.InvitedByUserId,
                p.ShoppingList.Items.Count,
                p.ShoppingList.Items.Count(i => i.IsChecked),
                p.ShoppingList.IsArchived))
            .ToList();
    }

    public async Task<ListSummaryDto> AcceptShareAsync(
        Guid permissionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var permission = await db.SharedPermissions
            .Include(p => p.ShoppingList)
                .ThenInclude(l => l.Items)
            .Include(p => p.ShoppingList)
                .ThenInclude(l => l.Owner)
            .Include(p => p.ShoppingList)
                .ThenInclude(l => l.SharedPermissions)
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
        permission.ShoppingList.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ListAccessService.ToSummary(permission.ShoppingList, userId);
    }

    public async Task DeclineShareAsync(
        Guid permissionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var permission = await db.SharedPermissions
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

        await listHubNotifier.EvictUserFromListAsync(userId, permission.ShoppingListId, cancellationToken);
    }

    public async Task LeaveListAsync(
        Guid listId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var list = await db.ShoppingLists
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (list is null)
        {
            throw new InvalidOperationException("List not found.");
        }

        if (list.OwnerId == userId)
        {
            throw new InvalidOperationException("List owners cannot leave their own list.");
        }

        var permission = await db.SharedPermissions
            .FirstOrDefaultAsync(
                p => p.ShoppingListId == listId && p.UserId == userId,
                cancellationToken);

        if (permission is null || permission.Status != ListShareStatus.Accepted)
        {
            throw new InvalidOperationException("You are not a member of this list.");
        }

        db.SharedPermissions.Remove(permission);
        await db.SaveChangesAsync(cancellationToken);

        await listHubNotifier.EvictUserFromListAsync(userId, listId, cancellationToken);
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
