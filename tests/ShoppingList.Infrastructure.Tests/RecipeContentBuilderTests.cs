using ShoppingList.Application.Parsing;
using ShoppingList.Application.Recipes;
using Xunit;

namespace ShoppingList.Infrastructure.Tests;

public class RecipeContentBuilderTests
{
    [Fact]
    public void InferSectionGroups_splits_allrecipes_style_list_at_confectioners_sugar()
    {
        var ingredients = new List<ParsedIngredientDto>
        {
            new("All-purpose flour", "2 cups", "Pantry"),
            new("Eggs", "2 large", "Dairy"),
            new("Sour cream", "1/2 cup", "Dairy"),
            new("Lemon extract", "1 teaspoon", "Seasonings"),
            new("Lemon zest", "1 teaspoon", "Seasonings"),
            new("Baking soda", "1 teaspoon", "Seasonings"),
            new("Salt", "1/2 teaspoon", "Seasonings"),
            new("Confectioners sugar, plus more as needed", "2 cups", "Pantry"),
            new("1% milk", "2 tablespoons", "Dairy"),
            new("Salted butter", "6 tablespoons", "Dairy"),
        };

        var content = RecipeContentBuilder.FromParsed(new ParsedRecipeContentDto(ingredients, []));

        Assert.Equal(2, content.Recipe.SubCategories.Count);
        Assert.Equal("Cake", content.Recipe.SubCategories[0].Description);
        Assert.Equal("Glaze", content.Recipe.SubCategories[1].Description);
        Assert.Equal(7, content.Recipe.SubCategories[0].Ingredients.Count);
        Assert.Equal(3, content.Recipe.SubCategories[1].Ingredients.Count);
    }

    [Fact]
    public void FromParsed_does_not_keep_placeholder_ingredients_section_beside_named_sections()
    {
        var ingredients = new List<ParsedIngredientDto>
        {
            new("Salted butter", "1 cup", "Dairy"),
            new("All-purpose flour", "2 cups", "Pantry", "Cake"),
            new("Eggs", "2 large", "Dairy", "Cake"),
            new("Confectioners sugar", "2 cups", "Pantry", "Glaze"),
        };

        var content = RecipeContentBuilder.FromParsed(new ParsedRecipeContentDto(ingredients, []));

        Assert.Equal(2, content.Recipe.SubCategories.Count);
        Assert.DoesNotContain(
            content.Recipe.SubCategories,
            block => block.Description.Equals("Ingredients", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Cake", content.Recipe.SubCategories[0].Description);
        Assert.Contains(
            content.Recipe.SubCategories[0].Ingredients,
            line => line.Contains("butter", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Glaze", content.Recipe.SubCategories[1].Description);
    }
}
