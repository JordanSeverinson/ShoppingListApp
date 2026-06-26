using ShoppingList.Application.Hubs;

namespace ShoppingList.Api.Contracts;

public record ListSummaryResponse(
    IReadOnlyList<ListSummaryDto> SharedLists,
    IReadOnlyList<ListSummaryDto> HistoricalLists);

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
