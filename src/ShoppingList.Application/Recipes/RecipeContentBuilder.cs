using ShoppingList.Application.Parsing;
using ShoppingList.Domain.Entities;
using ShoppingList.Domain.Recipes;

namespace ShoppingList.Application.Recipes;

public static class RecipeContentBuilder
{
    public static RecipeContentDocument FromParsed(ParsedRecipeContentDto parsed)
    {
        var ingredients = parsed.Ingredients.ToList();
        InferSectionGroups(ingredients);
        FoldUnlabeledIntoFirstNamedSection(ingredients);

        return new RecipeContentDocument
        {
            Recipe = new RecipeContentRoot
            {
                SubCategories = BuildSubCategories(ingredients),
                CookingSteps = parsed.Steps.ToList()
            }
        };
    }

    public static RecipeContentDocument FromEntities(
        IEnumerable<RecipeIngredient> ingredients,
        IEnumerable<RecipeStep> steps)
    {
        var ingredientDtos = ingredients
            .OrderBy(i => i.SortOrder)
            .Select(i => new ParsedIngredientDto(
                i.Name,
                i.Quantity ?? string.Empty,
                i.Category,
                i.Section))
            .ToList();

        InferSectionGroups(ingredientDtos);

        return new RecipeContentDocument
        {
            Recipe = new RecipeContentRoot
            {
                SubCategories = BuildSubCategories(ingredientDtos),
                CookingSteps = steps.OrderBy(s => s.SortOrder).Select(s => s.Text).ToList()
            }
        };
    }

    /// <summary>
    /// When OCR misses section headers (common on AllRecipes screenshots), infer Cake/Glaze
    /// split at the first typical glaze ingredient.
    /// </summary>
    internal static void InferSectionGroups(List<ParsedIngredientDto> ingredients)
    {
        if (ingredients.Any(i => !RecipeSectionNames.IsDefault(i.Section)))
        {
            return;
        }

        var glazeStart = ingredients.FindIndex(IsLikelyGlazeIngredient);
        if (glazeStart <= 0 || glazeStart >= ingredients.Count - 1)
        {
            return;
        }

        for (var i = 0; i < ingredients.Count; i++)
        {
            var section = i < glazeStart ? "Cake" : "Glaze";
            ingredients[i] = ingredients[i] with { Section = section };
        }
    }

    /// <summary>
    /// OCR often leaves items above the first real header (Cake, Glaze, …) unlabeled
    /// after skipping the document "Ingredients" heading. Fold those into the first
    /// named section instead of saving a placeholder "Ingredients" group.
    /// </summary>
    public static void FoldUnlabeledIntoFirstNamedSection(List<ParsedIngredientDto> ingredients)
    {
        var firstNamed = ingredients
            .Select(ingredient => ingredient.Section?.Trim())
            .FirstOrDefault(section => !RecipeSectionNames.IsDefault(section));

        if (string.IsNullOrEmpty(firstNamed))
        {
            return;
        }

        for (var i = 0; i < ingredients.Count; i++)
        {
            if (RecipeSectionNames.IsDefault(ingredients[i].Section))
            {
                ingredients[i] = ingredients[i] with { Section = firstNamed };
            }
        }
    }

    private static bool IsLikelyGlazeIngredient(ParsedIngredientDto ingredient)
    {
        var text = $"{ingredient.Quantity} {ingredient.Name}".ToLowerInvariant();
        return text.Contains("confectioners", StringComparison.Ordinal)
            || text.Contains("confectioner's", StringComparison.Ordinal)
            || text.Contains("powdered sugar", StringComparison.Ordinal)
            || text.Contains("icing sugar", StringComparison.Ordinal);
    }

    public static List<RecipeSubCategoryBlock> BuildSubCategories(IEnumerable<ParsedIngredientDto> ingredients)
    {
        var blocks = new List<RecipeSubCategoryBlock>();
        var sectionOrder = new List<string>();
        var linesBySection = new Dictionary<string, List<string>>();

        foreach (var ingredient in ingredients)
        {
            var sectionKey = RecipeSectionNames.Persist(ingredient.Section) ?? string.Empty;

            if (!linesBySection.ContainsKey(sectionKey))
            {
                linesBySection[sectionKey] = [];
                sectionOrder.Add(sectionKey);
            }

            linesBySection[sectionKey].Add(FormatIngredientLine(ingredient));
        }

        if (sectionOrder.Count == 0)
        {
            return blocks;
        }

        foreach (var sectionKey in sectionOrder)
        {
            blocks.Add(new RecipeSubCategoryBlock
            {
                Description = string.IsNullOrEmpty(sectionKey) ? RecipeSectionNames.Default : sectionKey,
                Ingredients = linesBySection[sectionKey]
            });
        }

        return blocks;
    }

    public static string FormatIngredientLine(ParsedIngredientDto ingredient)
    {
        if (string.IsNullOrWhiteSpace(ingredient.Quantity))
        {
            return ingredient.Name.Trim();
        }

        return $"{ingredient.Quantity.Trim()} {ingredient.Name.Trim()}".Trim();
    }

    public static void SetCookingSteps(RecipeContentDocument content, IReadOnlyList<string> steps)
    {
        content.Recipe.CookingSteps = steps
            .Select(step => step.Trim())
            .Where(step => step.Length > 0)
            .ToList();
    }

    public static RecipeContentDocument MergeImportedContent(
        RecipeContentDocument existing,
        RecipeContentDocument imported)
    {
        if (imported.Recipe.SubCategories.Count > 0)
        {
            existing.Recipe.SubCategories = imported.Recipe.SubCategories
                .Select(block => new RecipeSubCategoryBlock
                {
                    Description = block.Description,
                    Ingredients = block.Ingredients.ToList()
                })
                .ToList();
        }

        if (imported.Recipe.CookingSteps.Count > 0)
        {
            existing.Recipe.CookingSteps = imported.Recipe.CookingSteps.ToList();
        }

        return existing;
    }
}
