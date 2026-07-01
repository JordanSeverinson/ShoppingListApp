using Microsoft.AspNetCore.SignalR;
using ShoppingList.Api.Hubs;
using ShoppingList.Api.Security;

namespace ShoppingList.Api.Services;

public class ListHubNotifier(
    IHubContext<ShoppingListHub> hubContext,
    HubConnectionTracker tracker,
    ILogger<ListHubNotifier> logger)
{
    public async Task EvictUserFromListAsync(
        Guid userId,
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        var connectionIds = tracker.GetConnectionIdsForUserInList(userId, listId);
        if (connectionIds.Count == 0)
        {
            return;
        }

        var groupName = ShoppingListHub.GroupName(listId);
        foreach (var connectionId in connectionIds)
        {
            await hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);
            tracker.TrackLeave(connectionId, listId);
        }

        SecurityAuditLogger.LogListShareRevoked(logger, userId, listId);
    }

    public async Task EvictAllFromListAsync(
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        var connectionIds = tracker.GetConnectionIdsInList(listId);
        if (connectionIds.Count == 0)
        {
            return;
        }

        var groupName = ShoppingListHub.GroupName(listId);
        foreach (var connectionId in connectionIds)
        {
            await hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);
            tracker.TrackLeave(connectionId, listId);
        }
    }
}
