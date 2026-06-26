namespace ShoppingList.Application.Hubs;

public record ListItemEventDto(
    Guid Id,
    Guid ShoppingListId,
    string Name,
    string? Quantity,
    string Category,
    bool IsChecked,
    int SortOrder);
