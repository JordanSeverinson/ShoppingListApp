using ShoppingList.Application.Hubs;

namespace ShoppingList.Api.Contracts;

public record ListSummaryResponse(
    IReadOnlyList<ListSummaryDto> SharedLists,
    IReadOnlyList<ListSummaryDto> ArchivedLists);

public record ListSummaryDto(
    Guid Id,
    string Name,
    string ShareCode,
    bool IsArchived,
    bool IsOwner,
    DateTime? UpdatedAt,
    int ItemCount,
    int CheckedCount);

public record ListDetailResponse(
    Guid Id,
    string Name,
    string ShareCode,
    bool IsArchived,
    bool CanEdit,
    IReadOnlyList<ListItemEventDto> Items);

public record CreateListRequest(string Name);

public record JoinListRequest(string ShareCode);

public record RenameListRequest(string Name);

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
