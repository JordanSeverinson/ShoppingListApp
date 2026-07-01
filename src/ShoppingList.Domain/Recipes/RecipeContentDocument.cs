namespace ShoppingList.Domain.Recipes;

public class RecipeContentDocument
{
    public RecipeContentRoot Recipe { get; set; } = new();
}

public class RecipeContentRoot
{
    public List<RecipeSubCategoryBlock> SubCategories { get; set; } = [];

    public List<string> CookingSteps { get; set; } = [];
}

public class RecipeSubCategoryBlock
{
    public string Description { get; set; } = string.Empty;

    public List<string> Ingredients { get; set; } = [];
}
