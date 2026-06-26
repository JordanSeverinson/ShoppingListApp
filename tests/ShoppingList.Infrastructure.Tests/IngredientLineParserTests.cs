using ShoppingList.Infrastructure.Ocr;
using Xunit;

namespace ShoppingList.Infrastructure.Tests;

public class IngredientLineParserTests
{
    private const string SampleOcr = """
        Ingredients
        3 Tbsp. extra-virgin olive oil, divided
        4 boneless, skinless chicken breasts
        Kosher salt
        Freshly ground black pepper
        2 garlic cloves, finely chopped
        1 Tbsp. fresh thyme leaves
        1 tsp. crushed red pepper flakes
        3/4 cup low-sodium chicken broth
        1/2 cup finely chopped sun-dried tomatoes
        1/2 cup heavy cream
        1/4 cup finely grated Parmesan
        Torn fresh basil, for serving
        """;

    [Fact]
    public void Parse_recipe_list_extracts_full_ingredient_names()
    {
        var results = IngredientLineParser.Parse(SampleOcr);

        Assert.Contains(results, i => i.Name.Contains("olive oil", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, i => i.Name.Contains("chicken", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, i => i.Name.Equals("Kosher salt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, i => i.Name.Contains("sun-dried", StringComparison.OrdinalIgnoreCase));
        Assert.True(results.Count >= 10);
    }

    [Fact]
    public void Parse_merges_hyphenated_line_breaks()
    {
        var ocr = """
            1/2 cup finely chopped sun-
            dried tomatoes
            """;

        var results = IngredientLineParser.Parse(ocr);

        Assert.Single(results);
        Assert.Contains("sun-dried tomatoes", results[0].Name, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("1/2 cup", results[0].Quantity);
    }
}
