using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ShoppingList.Application.Hubs;
using ShoppingList.Api.Services;

namespace ShoppingList.Api.Hubs;

/// <summary>
/// Real-time channel for collaborative list updates.
/// Clients join a list group via <see cref="JoinList"/> and receive:
/// ItemAdded, ItemUpdated, ItemToggled, ItemDeleted, ItemsBulkAdded, ItemsBulkDeleted.
/// </summary>
[Authorize]
public class ShoppingListHub(
    ListAccessService listAccess,
    CurrentUserService currentUser,
    HubConnectionTracker connectionTracker) : Hub
{
    public static string GroupName(Guid listId) => $"list:{listId}";

    public async Task JoinList(Guid listId)
    {
        var userId = currentUser.GetUserId();
        var list = await listAccess.GetAccessibleListMetadataAsync(listId, userId, Context.ConnectionAborted);
        if (list is null)
        {
            throw new HubException("Access denied.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(listId));
        connectionTracker.TrackJoin(Context.ConnectionId, userId, listId);
    }

    public async Task LeaveList(Guid listId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(listId));
        connectionTracker.TrackLeave(Context.ConnectionId, listId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var listIds = connectionTracker.GetListIds(Context.ConnectionId);
        foreach (var listId in listIds)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(listId));
        }

        connectionTracker.Disconnect(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public static Task ItemAdded(IHubContext<ShoppingListHub> hub, Guid listId, ListItemEventDto item) =>
        hub.Clients.Group(GroupName(listId)).SendAsync("ItemAdded", item);

    public static Task ItemUpdated(IHubContext<ShoppingListHub> hub, Guid listId, ListItemEventDto item) =>
        hub.Clients.Group(GroupName(listId)).SendAsync("ItemUpdated", item);

    public static Task ItemToggled(IHubContext<ShoppingListHub> hub, Guid listId, Guid itemId, bool isChecked) =>
        hub.Clients.Group(GroupName(listId)).SendAsync("ItemToggled", itemId, isChecked);

    public static Task ItemDeleted(IHubContext<ShoppingListHub> hub, Guid listId, Guid itemId) =>
        hub.Clients.Group(GroupName(listId)).SendAsync("ItemDeleted", itemId);

    public static Task ItemsBulkAdded(IHubContext<ShoppingListHub> hub, Guid listId, IReadOnlyList<ListItemEventDto> items) =>
        hub.Clients.Group(GroupName(listId)).SendAsync("ItemsBulkAdded", items);

    public static Task ItemsBulkToggled(
        IHubContext<ShoppingListHub> hub,
        Guid listId,
        IReadOnlyList<Guid> itemIds,
        bool isChecked) =>
        hub.Clients.Group(GroupName(listId)).SendAsync("ItemsBulkToggled", itemIds, isChecked);

    public static Task ItemsBulkDeleted(IHubContext<ShoppingListHub> hub, Guid listId, IReadOnlyList<Guid> itemIds) =>
        hub.Clients.Group(GroupName(listId)).SendAsync("ItemsBulkDeleted", itemIds);
}
