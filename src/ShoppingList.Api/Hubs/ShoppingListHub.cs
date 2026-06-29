using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ShoppingList.Application.Hubs;

namespace ShoppingList.Api.Hubs;

/// <summary>
/// Real-time channel for collaborative list updates.
/// Clients join a list group via <see cref="JoinList"/> and receive:
/// ItemAdded, ItemUpdated, ItemToggled, ItemDeleted, ItemsBulkAdded, ItemsBulkDeleted.
/// </summary>
[AllowAnonymous]
public class ShoppingListHub : Hub
{
    public static string GroupName(Guid listId) => $"list:{listId}";

    public async Task JoinList(Guid listId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(listId));
    }

    public async Task LeaveList(Guid listId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(listId));
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
