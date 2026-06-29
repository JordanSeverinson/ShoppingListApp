using ShoppingList.Application.Recipes;

namespace ShoppingList.Api.Contracts;

public record RecipeSummaryResponse(IReadOnlyList<RecipeSummaryDto> Recipes);

public record RecipeSummaryDto(
    Guid Id,
    string Name,
    string ShareCode,
    bool IsOwner,
    DateTime? UpdatedAt,
    int IngredientCount);

public record RecipeDetailResponse(
    Guid Id,
    string Name,
    string ShareCode,
    IReadOnlyList<RecipeIngredientDto> Ingredients);

public record CreateRecipeRequest(string Name);

public record JoinRecipeRequest(string ShareCode);

public record RenameRecipeRequest(string Name);

public record CreateRecipeIngredientRequest(string Name, string? Quantity, string Category);

public record DeleteRecipeIngredientsRequest(IReadOnlyList<Guid> IngredientIds);

public record DeleteRecipeIngredientsResponse(int DeletedCount, IReadOnlyList<Guid> IngredientIds);

public record ImportRecipeResponse(
    Guid ListId,
    IReadOnlyList<ShoppingList.Application.Hubs.ListItemEventDto> Items,
    string Message);

public record UploadRecipeImageResponse(
    Guid RecipeId,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    string Message);
