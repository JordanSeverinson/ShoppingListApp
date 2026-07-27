using ShoppingList.Application.Recipes;
using ShoppingList.Domain.Recipes;

namespace ShoppingList.Api.Contracts;

public record RecipeSummaryResponse(
    IReadOnlyList<RecipeSummaryDto> Recipes,
    IReadOnlyList<PendingRecipeShareDto> PendingShares);

public record RecipeSummaryDto(
    Guid Id,
    string Name,
    string? RecipeType,
    bool IsOwner,
    DateTime? UpdatedAt,
    int IngredientCount);

public record PendingRecipeShareDto(
    Guid Id,
    Guid RecipeId,
    string RecipeName,
    string InvitedByName,
    Guid InvitedByUserId,
    int IngredientCount);

public record RecipeDetailResponse(
    Guid Id,
    string Name,
    string? RecipeType,
    bool IsOwner,
    RecipeContentDocument Content,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps);

public record SaveRecipeContentRequest(RecipeContentDocument Content);

public record CreateRecipeRequest(string Name, string? RecipeType);

public record UpdateRecipeRequest(string? Name, string? RecipeType);

public record ShareRecipeRequest(IReadOnlyList<Guid> FriendUserIds);

public record ShareRecipeResponse(int InvitedCount, int SkippedCount, string Message);

public record CreateRecipeIngredientRequest(string Name, string? Quantity, string Category, string? Section);

public record UpdateRecipeIngredientRequest(
    string Name,
    string? Quantity,
    string Category,
    string? Section,
    int? SortOrder);

public record RenameRecipeSectionRequest(string From, string To);

public record ReplaceRecipeStepsRequest(IReadOnlyList<string> Steps);

public record DeleteRecipeIngredientsRequest(IReadOnlyList<Guid> IngredientIds);

public record DeleteRecipeIngredientsResponse(int DeletedCount, IReadOnlyList<Guid> IngredientIds);

public record ImportRecipeResponse(
    Guid ListId,
    IReadOnlyList<ShoppingList.Application.Hubs.ListItemEventDto> Items,
    string Message);

public record UploadRecipeImageResponse(
    Guid RecipeId,
    RecipeContentDocument Content,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps,
    string Message);
