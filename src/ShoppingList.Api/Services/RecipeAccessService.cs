using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Application.Recipes;
using ShoppingList.Domain.Entities;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class RecipeAccessService(ApplicationDbContext db)
{
    public IQueryable<Recipe> AccessibleRecipes(Guid userId) =>
        db.Recipes.Where(r =>
            r.OwnerId == userId
            || r.SharedPermissions.Any(p => p.UserId == userId));

    public async Task<Recipe?> GetAccessibleRecipeAsync(
        Guid recipeId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await AccessibleRecipes(userId)
            .Include(r => r.Ingredients)
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
            recipe.ShareCode,
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
            ingredient.SortOrder);
}
