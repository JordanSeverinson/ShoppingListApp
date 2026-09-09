using ShoppingList.Application.Parsing;
using ShoppingList.Application.Recipes;
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
    public void ParseRecipeContent_merges_wrapped_ingredient_notes()
    {
        const string ocr = """
            Ingredients
            Glaze
            2 cups confectioners sugar, plus more as
            needed
            2 tablespoons 1% milk
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr);

        Assert.Contains(content.Ingredients, i =>
            i.Name.Contains("confectioners", StringComparison.OrdinalIgnoreCase)
            && i.Name.Contains("needed", StringComparison.OrdinalIgnoreCase)
            && i.Quantity.Contains("2 cups", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseRecipeContent_allrecipes_full_list_imports_sixteen_ingredients()
    {
        const string ocr = """
            Ingredients
            1/2X
            1X
            2X
            Original recipe (1X) yields 20 servings
            Cake
            1 cup butter
            1 cup water
            2 cups all-purpose flour
            2 cups white sugar
            2 large eggs
            1/2 cup sour cream
            1 teaspoon lemon extract
            1 teaspoon lemon zest
            1 teaspoon baking soda
            1/2 teaspoon salt
            Glaze
            2 cups confectioners sugar, plus more as needed
            2 tablespoons 1% milk
            6 tablespoons salted butter
            1/4 teaspoon kosher salt
            1/2 teaspoon lemon extract
            2 tablespoons lemon zest
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr);

        Assert.Equal(16, content.Ingredients.Count);
        Assert.Contains(content.Ingredients, i => i.Name.Contains("butter", StringComparison.OrdinalIgnoreCase) && i.Quantity.Contains("cup", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(content.Ingredients, i => i.Name.Contains("all-purpose flour", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(content.Ingredients, i => i.Name.Contains("confectioners", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseRecipeContent_recovers_from_common_ocr_digit_errors()
    {
        const string ocr = """
            Ingredients
            Cake
            l cup butter
            I cup water
            2 cups all-purpose flour
            2 large eggs
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr);

        Assert.Contains(content.Ingredients, i =>
            i.Name.Contains("butter", StringComparison.OrdinalIgnoreCase) && i.Quantity.StartsWith("1", StringComparison.Ordinal));
        Assert.Contains(content.Ingredients, i =>
            i.Name.Contains("water", StringComparison.OrdinalIgnoreCase) && i.Quantity.StartsWith("1", StringComparison.Ordinal));
    }

    [Fact]
    public void ParseRecipeContent_allrecipes_style_builds_sectioned_json()
    {
        const string ocr = """
            Ingredients
            Cake
            1 cup butter
            2 large eggs
            Glaze
            2 cups confectioners sugar, plus more as needed
            Directions
            1. Preheat the oven to 350 degrees F (175 degrees C).
            """;

        var content = RecipeContentBuilder.FromParsed(IngredientLineParser.ParseRecipeContent(ocr));

        Assert.Equal(2, content.Recipe.SubCategories.Count);
        Assert.DoesNotContain(content.Recipe.SubCategories, b =>
            string.Equals(b.Description, "Ingredients", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Cake", content.Recipe.SubCategories[0].Description);
        Assert.Contains(content.Recipe.SubCategories[0].Ingredients, i => i.Contains("butter", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Glaze", content.Recipe.SubCategories[1].Description);
        var step = Assert.Single(content.Recipe.CookingSteps);
        Assert.Contains("Preheat", step, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseRecipeContent_allrecipes_step_headers_split_into_separate_steps()
    {
        const string ocr = """
            Ingredients
            Cake
            1 cup butter
            Glaze
            2 cups confectioners sugar, plus more as needed
            Directions
            Step 1
            Preheat the oven to 375 degrees F (190 degrees C). Grease a 10x15-inch baking pan.
            Step 2
            Bring 1 cup butter and water to a boil in a large saucepan. Remove from heat, and stir in flour, sugar, eggs, sour cream, lemon extract, baking soda, and salt until smooth. Pour batter into the prepared pan and spread into an even layer.
            Step 3
            Bake in the preheated oven until cake is golden and a toothpick inserted near the center comes out clean, 20 to 22 minutes. Cool for 15 minutes.
            Step 4
            Meanwhile, for glaze, place confectioner's sugar in a bowl.
            Step 5
            Add milk, butter, and salt to a small saucepan over medium heat, and bring just to a boil, stirring occasionally; pour over confectioner's sugar and whisk to combine. If glaze is too thin, whisk in more confectioner's sugar. Whisk in lemon extract and lemon zest.
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr);

        Assert.Equal(5, content.Steps.Count);
        Assert.Contains("Preheat", content.Steps[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("375", content.Steps[0], StringComparison.Ordinal);
        Assert.Contains("Bring", content.Steps[1], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Bake", content.Steps[2], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("glaze", content.Steps[3], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Whisk in lemon extract", content.Steps[4], StringComparison.OrdinalIgnoreCase);
        Assert.False(content.Steps[0].StartsWith("Step", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseRecipeContent_splits_inline_step_markers_on_one_line()
    {
        const string ocr = """
            Directions
            Step 1 Preheat the oven to 350 degrees F. Step 2 Combine butter and water in a saucepan.
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr);

        Assert.Equal(2, content.Steps.Count);
        Assert.Contains("Preheat", content.Steps[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Combine", content.Steps[1], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseRecipeContent_allrecipes_style_imports_flat_ingredients_and_steps()
    {
        const string ocr = """
            Ingredients
            1/2X
            1X
            2X
            Original recipe (1X) yields 20 servings
            Cake
            1 cup butter
            1 cup water
            2 cups all-purpose flour
            2 cups white sugar
            2 large eggs
            Glaze
            2 cups confectioners sugar, plus more as needed
            2 tablespoons 1% milk
            Directions
            1. Preheat the oven to 350 degrees F (175 degrees C).
            2. Combine butter and water in a saucepan; bring to a boil.
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr);

        Assert.Contains(content.Ingredients, i => i.Section == "Cake");
        Assert.Contains(content.Ingredients, i => i.Section == "Glaze");
        Assert.Contains(content.Ingredients, i =>
            i.Name.Contains("butter", StringComparison.OrdinalIgnoreCase)
            && i.Quantity.Contains("1 cup", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(content.Ingredients, i =>
            i.Name.Contains("confectioners", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(content.Ingredients, i =>
            i.Name.Contains("eggs", StringComparison.OrdinalIgnoreCase));
        Assert.True(content.Ingredients.Count >= 7);
        Assert.Equal(2, content.Steps.Count);
        Assert.Contains("Preheat", content.Steps[0], StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void ParseRecipeContent_ingredients_only_stops_at_directions()
    {
        const string ocr = """
            Ingredients
            1 cup butter
            2 cups flour
            Directions
            1. Preheat the oven.
            2. Mix ingredients.
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr, RecipeImageImportMode.IngredientsOnly);

        Assert.Equal(2, content.Ingredients.Count);
        Assert.Empty(content.Steps);
    }

    [Fact]
    public void ParseRecipeContent_steps_only_skips_ingredients()
    {
        const string ocr = """
            Ingredients
            1 cup butter
            Directions
            Step 1
            Preheat the oven to 350 degrees F.
            Step 2
            Combine butter and water in a saucepan.
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr, RecipeImageImportMode.CookingStepsOnly);

        Assert.Empty(content.Ingredients);
        Assert.Equal(2, content.Steps.Count);
        Assert.Contains("Preheat", content.Steps[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseRecipeContent_ingredients_only_filters_checkbox_ocr_noise()
    {
        const string ocr = """
            Ingredients
            0 O4 d O oo O
            3 Tbsp. extra-virgin olive oil, divided
            4 boneless, skinless chicken breasts
            Kosher salt
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr, RecipeImageImportMode.IngredientsOnly);

        Assert.Equal(3, content.Ingredients.Count);
        Assert.DoesNotContain(content.Ingredients, i => i.Name.Contains("O4", StringComparison.Ordinal));
        Assert.Contains(content.Ingredients, i => i.Name.Contains("olive oil", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(content.Ingredients, i => i.Name.Contains("chicken", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseRecipeContent_steps_only_splits_bare_numbered_steps()
    {
        const string ocr = """
            1 Dress the tomatoes, onions, and cucumber with olive oil, red wine vinegar, salt, and pepper.
            2 Let stand while you prepare dinner, about 20 minutes. Re-toss and serve salad with crusty bread for mopping up juices and oil.
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr, RecipeImageImportMode.CookingStepsOnly);

        Assert.Equal(2, content.Steps.Count);
        Assert.Contains("Dress", content.Steps[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Let stand", content.Steps[1], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("1 Dress", content.Steps[0], StringComparison.Ordinal);
    }

    [Fact]
    public void ParseRecipeContent_does_not_treat_document_ingredients_heading_as_a_section()
    {
        const string ocr = """
            Ingredients
            1 cup butter
            Cake
            2 cups all-purpose flour
            Glaze
            2 cups confectioners sugar
            """;

        var parsed = IngredientLineParser.ParseRecipeContent(ocr);
        var content = RecipeContentBuilder.FromParsed(parsed);

        Assert.DoesNotContain(parsed.Ingredients, i =>
            string.Equals(i.Section, "Ingredients", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(parsed.Ingredients, i =>
            i.Name.Contains("butter", StringComparison.OrdinalIgnoreCase)
            && i.Section == "Cake");
        Assert.Equal(2, content.Recipe.SubCategories.Count);
        Assert.DoesNotContain(content.Recipe.SubCategories, b =>
            string.Equals(b.Description, "Ingredients", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseRecipeContent_steps_only_splits_inline_bare_numbered_steps_on_one_line()
    {
        const string ocr = """
            1 Dress the tomatoes, onions, and cucumber with olive oil, red wine vinegar, salt, and pepper. 2 Let stand while you prepare dinner, about 20 minutes.
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr, RecipeImageImportMode.CookingStepsOnly);

        Assert.Equal(2, content.Steps.Count);
        Assert.Contains("Dress", content.Steps[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Let stand", content.Steps[1], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseRecipeContent_steps_only_splits_standalone_step_numbers()
    {
        const string ocr = """
            1
            Dress the tomatoes, onions, and cucumber with olive oil, red wine vinegar, salt, and pepper.
            2
            Let stand while you prepare dinner, about 20 minutes.
            """;

        var content = IngredientLineParser.ParseRecipeContent(ocr, RecipeImageImportMode.CookingStepsOnly);

        Assert.Equal(2, content.Steps.Count);
        Assert.Contains("Dress", content.Steps[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Let stand", content.Steps[1], StringComparison.OrdinalIgnoreCase);
    }
}
