namespace ShoppingList.Application.Recipes;

public static class RecipeSectionNames
{
    public const string Default = "Ingredients";

    public static bool IsDefault(string? section) =>
        string.IsNullOrWhiteSpace(section)
        || section.Trim().Equals(Default, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Persist unnamed / placeholder groups as null so "Ingredients" is display-only.
    /// </summary>
    public static string? Persist(string? section)
    {
        var trimmed = section?.Trim();
        return IsDefault(trimmed) ? null : trimmed;
    }
}
