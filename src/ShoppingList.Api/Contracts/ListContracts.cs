using ShoppingList.Application.Hubs;

namespace ShoppingList.Api.Contracts;

public record ListSummaryResponse(
    IReadOnlyList<ListSummaryDto> ActiveLists,
    IReadOnlyList<ListSummaryDto> ArchivedLists,
    IReadOnlyList<PendingListShareDto> PendingShares);

public record ListSummaryDto(
    Guid Id,
    string Name,
    bool IsArchived,
    bool IsOwner,
    DateTime? UpdatedAt,
    int ItemCount,
    int CheckedCount,
    int AcceptedShareCount,
    string OwnerDisplayName);

public record PendingListShareDto(
    Guid Id,
    Guid ListId,
    string ListName,
    string InvitedByName,
    Guid InvitedByUserId,
    int ItemCount,
    int CheckedCount,
    bool IsArchived);

public record ListDetailResponse(
    Guid Id,
    string Name,
    bool IsArchived,
    bool IsOwner,
    bool CanEdit,
    IReadOnlyList<ListItemEventDto> Items);

public record CreateListRequest(string Name);

public record RenameListRequest(string Name);

public record ShareListRequest(IReadOnlyList<Guid> FriendUserIds);

public record ShareListResponse(int InvitedCount, int SkippedCount, string Message);

public record CreateItemRequest(string Name, string? Quantity, string Category);

public record UpdateItemRequest(
    string? Name,
    string? Quantity,
    string? Category,
    bool? IsChecked);

public record CheckAllItemsRequest(string? Category);

public record CheckAllItemsResponse(int UpdatedCount, IReadOnlyList<Guid> ItemIds);

public record DeleteItemsRequest(IReadOnlyList<Guid> ItemIds);

public record DeleteItemsResponse(int DeletedCount, IReadOnlyList<Guid> ItemIds);
