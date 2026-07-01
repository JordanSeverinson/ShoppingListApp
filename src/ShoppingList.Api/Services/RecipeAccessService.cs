using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Application.Recipes;
using ShoppingList.Domain.Entities;
using ShoppingList.Domain.Enums;
using ShoppingList.Domain.Recipes;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class RecipeAccessService(ApplicationDbContext db)
{
    public IQueryable<Recipe> AccessibleRecipes(Guid userId) =>
        db.Recipes.Where(r =>
            r.OwnerId == userId
            || r.SharedPermissions.Any(p =>
                p.UserId == userId && p.Status == ListShareStatus.Accepted));

    public async Task<Recipe?> GetAccessibleRecipeAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await AccessibleRecipes(userId)
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);

    public async Task<Recipe?> GetAccessibleRecipeMetadataAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await AccessibleRecipes(userId)
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);

    public static RecipeSummaryDto ToSummary(Recipe recipe, Guid userId) =>
        new(
            recipe.Id,
            recipe.Name,
            recipe.OwnerId == userId,
            recipe.UpdatedAt ?? recipe.CreatedAt,
            recipe.Ingredients.Count);

    public static RecipeIngredientDto ToIngredientDto(RecipeIngredient ingredient) =>
        new(
            ingredient.Id,
            ingredient.RecipeId,
            ingredient.Name,
            ingredient.Quantity,
            ingredient.Category,
            ingredient.Section,
            ingredient.SortOrder);

    public static RecipeStepDto ToStepDto(RecipeStep step) =>
        new(step.Id, step.RecipeId, step.Text, step.SortOrder);

    public static RecipeContentDocument ResolveContent(Recipe recipe)
    {
        var rebuilt = RecipeContentBuilder.FromEntities(recipe.Ingredients, recipe.Steps);
        if (rebuilt.Recipe.SubCategories.Count > 1)
        {
            return rebuilt;
        }

        var stored = recipe.Content;
        if (stored.Recipe.SubCategories.Count > 1)
        {
            return stored;
        }

        if (HasStoredContent(stored))
        {
            return stored;
        }

        if (recipe.Ingredients.Count == 0 && recipe.Steps.Count == 0)
        {
            return stored;
        }

        return rebuilt;
    }

    private static bool HasStoredContent(RecipeContentDocument content) =>
        content.Recipe.SubCategories.Count > 0 || content.Recipe.CookingSteps.Count > 0;
}
