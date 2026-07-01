namespace ShoppingList.Application.Recipes;

public record RecipeStepDto(Guid Id, Guid RecipeId, string Text, int SortOrder);
