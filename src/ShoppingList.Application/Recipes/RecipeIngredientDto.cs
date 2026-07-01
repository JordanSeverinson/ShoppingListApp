namespace ShoppingList.Application.Recipes;

public record RecipeIngredientDto(
    Guid Id,
    Guid RecipeId,
    string Name,
    string? Quantity,
    string Category,
    string? Section,
    int SortOrder);
