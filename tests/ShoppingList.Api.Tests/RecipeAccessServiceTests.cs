using ShoppingList.Api.Services;
using Xunit;
using ShoppingList.Domain.Entities;
using ShoppingList.Domain.Recipes;

namespace ShoppingList.Api.Tests;

public class RecipeAccessServiceTests
{
    [Fact]
    public void ResolveContent_uses_stored_json_when_it_already_has_cooking_steps()
    {
        var recipe = new Recipe
        {
            Content = new RecipeContentDocument
            {
                Recipe = new RecipeContentRoot
                {
                    CookingSteps = ["Preheat", "Mix", "Bake"]
                }
            },
            Steps =
            [
                new RecipeStep { Text = "Mix", SortOrder = 1 },
                new RecipeStep { Text = "Bake", SortOrder = 2 },
            ]
        };

        var content = RecipeAccessService.ResolveContent(recipe);

        Assert.Equal(["Preheat", "Mix", "Bake"], content.Recipe.CookingSteps);
    }
}
