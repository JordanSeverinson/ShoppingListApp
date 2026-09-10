namespace ShoppingList.Application.Recipes;

public static class RecipeSectionNames
{
    public const string Default = "Ingredients";

    public static bool IsDefault(string? section) =>
        string.IsNullOrWhiteSpace(section)
        || section.Trim().Equals(Default, StringComparison.OrdinalIgnoreCase);

    public static bool IsUsable(string? section)
    {
        if (IsDefault(section))
        {
            return false;
        }

        var trimmed = section!.Trim();
        var letters = trimmed.Count(char.IsLetter);
        if (letters < 3)
        {
            return false;
        }

        var significant = trimmed.Count(c => !char.IsWhiteSpace(c));
        return letters >= significant * 0.6;
    }

    /// <summary>
    /// Persist unnamed / placeholder groups as null so "Ingredients" is display-only.
    /// </summary>
    public static string? Persist(string? section)
    {
        var trimmed = section?.Trim();
        return IsUsable(trimmed) ? trimmed : null;
    }
}
