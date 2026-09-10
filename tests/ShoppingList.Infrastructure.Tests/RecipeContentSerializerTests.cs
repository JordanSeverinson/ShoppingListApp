using ShoppingList.Application.Recipes;
using Xunit;
using ShoppingList.Domain.Recipes;

namespace ShoppingList.Infrastructure.Tests;

public class RecipeContentSerializerTests
{
    [Fact]
    public void Deserialize_reads_client_subCategories_array_without_losing_steps()
    {
        const string json = """
            {
              "recipe": {
                "cookingSteps": ["Mix", "Bake"],
                "subCategories": [
                  { "description": "Ingredients", "ingredients": ["2 cups flour"] }
                ]
              }
            }
            """;

        var content = RecipeContentSerializer.Deserialize(json);

        Assert.Equal(["Mix", "Bake"], content.Recipe.CookingSteps);
        var block = Assert.Single(content.Recipe.SubCategories);
        Assert.Equal("Ingredients", block.Description);
        Assert.Equal(["2 cups flour"], block.Ingredients);
    }

    [Fact]
    public void RoundTrip_keeps_numbered_subcategories_and_steps()
    {
        var original = new RecipeContentDocument
        {
            Recipe = new RecipeContentRoot
            {
                SubCategories =
                [
                    new RecipeSubCategoryBlock
                    {
                        Description = "Cake",
                        Ingredients = ["1 cup butter"]
                    }
                ],
                CookingSteps = ["Mix", "Bake"]
            }
        };

        var roundTripped = RecipeContentSerializer.Deserialize(RecipeContentSerializer.Serialize(original));

        Assert.Equal(["Mix", "Bake"], roundTripped.Recipe.CookingSteps);
        var block = Assert.Single(roundTripped.Recipe.SubCategories);
        Assert.Equal("Cake", block.Description);
    }
}
