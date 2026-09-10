namespace ShoppingList.Domain.Recipes;

public static class RecipeTypes
{
    public const string MainCourse = "Main Course";
    public const string SideDish = "Side Dish";
    public const string Snack = "Snack";
    public const string Dessert = "Dessert";
    public const string Drink = "Drink";

    public static readonly IReadOnlyList<string> All =
    [
        MainCourse,
        SideDish,
        Snack,
        Dessert,
        Drink,
    ];

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return All.FirstOrDefault(type =>
            string.Equals(type, trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
