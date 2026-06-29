using System.Text.RegularExpressions;
using ShoppingList.Application.Parsing;

namespace ShoppingList.Infrastructure.Ocr;

/// <summary>
/// Converts raw OCR text into structured ingredient rows.
/// </summary>
internal static partial class IngredientLineParser
{
    private static readonly string[] SkipLinePrefixes =
    [
        "ingredients", "directions", "instructions", "method", "preparation",
        "serves", "yield", "nutrition", "recipe", "notes", "optional"
    ];

    private static readonly HashSet<string> FragmentWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "divided", "breasts", "skinless", "boneless", "finely", "chopped", "grated", "fresh",
        "serving", "low-sodium", "extra-virgin", "crushed"
    };

    private static readonly (Regex Pattern, string Category)[] CategoryRules =
    [
        (SeasoningsPattern(), "Seasonings"),
        (ProducePattern(), "Produce"),
        (DairyPattern(), "Dairy"),
        (MeatPattern(), "Meat"),
        (BakeryPattern(), "Bakery"),
        (FrozenPattern(), "Frozen"),
        (PantryPattern(), "Pantry"),
    ];

    [GeneratedRegex(@"^[\-\*\u2022\u2023\u25E6\u2043\u25A1\u2610\u2611\u2612\[\]\(\)□■▪]+[\.\)\:]?\s*", RegexOptions.Compiled)]
    private static partial Regex BulletPrefixPattern();

    [GeneratedRegex(@"(\d)([A-Za-z])", RegexOptions.Compiled)]
    private static partial Regex DigitLetterPattern();

    [GeneratedRegex(@"(\d+\s*/\s*\d+|\d+(?:\.\d+)?)(?:\s*(cup|cups|tbsp|tsp|teaspoon|teaspoons|tablespoon|tablespoons|oz|ounce|ounces|lb|lbs|pound|pounds|g|gram|grams|kg|ml|l|liter|liters|can|cans|clove|cloves|slice|slices|piece|pieces|package|packages|pinch|dash|head|bunch|stalk|stalks|stick|sticks))?\.?\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex QuantityLinePattern();

    [GeneratedRegex(@"\b(lettuce|tomato|onion|garlic|apple|banana|carrot|celery|bell pepper|potato|spinach|broccoli|cucumber|lemon|lime|avocado|mushroom|zucchini|basil|cilantro|parsley|fruit|vegetable|salad|berries|thyme)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ProducePattern();

    [GeneratedRegex(@"\b(milk|cheese|butter|cream|yogurt|egg|eggs|sour cream|cheddar|mozzarella|parmesan|whipping cream|heavy cream)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DairyPattern();

    [GeneratedRegex(@"\b(chicken|beef|pork|turkey|bacon|sausage|ham|steak|ground beef|fish|salmon|shrimp|breasts|broth)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MeatPattern();

    [GeneratedRegex(@"\b(bread|bun|roll|tortilla|bagel|croissant|pita|baguette)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BakeryPattern();

    [GeneratedRegex(@"\b(frozen|ice cream)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex FrozenPattern();

    [GeneratedRegex(@"\b(salt|pepper|black pepper|chili powder|paprika|cumin|cinnamon|nutmeg|oregano|turmeric|curry powder|garlic powder|onion powder|seasoning|spice|spices|cayenne|vanilla|ginger powder|italian seasoning|red pepper flakes)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SeasoningsPattern();

    [GeneratedRegex(@"\b(flour|sugar|oil|vinegar|rice|pasta|beans|broth|stock|sauce|honey|syrup|nuts|oat|parmesan|tomatoes)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PantryPattern();

    [GeneratedRegex(@"[0O]{4,}|[^a-zA-Z0-9\s,\.\-/']{5,}", RegexOptions.Compiled)]
    private static partial Regex GarbagePattern();

    public static IReadOnlyList<ParsedIngredientDto> Parse(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
        {
            return [];
        }

        var rawLines = ocrText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeLine)
            .Where(line => line.Length >= 2 && !ShouldSkip(line) && !IsGarbage(line))
            .ToList();

        var mergedLines = MergeContinuationLines(rawLines);
        var results = new List<ParsedIngredientDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in mergedLines)
        {
            var (name, quantity) = ExtractNameAndQuantity(line);
            if (string.IsNullOrWhiteSpace(name) || IsLikelyFragment(name, quantity))
            {
                continue;
            }

            name = CleanIngredientName(name);
            quantity = CleanQuantity(quantity);

            var dedupeKey = $"{quantity}|{name}".ToLowerInvariant();
            if (!seen.Add(dedupeKey))
            {
                continue;
            }

            results.Add(new ParsedIngredientDto(
                Name: name,
                Quantity: quantity,
                Category: InferCategory(name)));
        }

        return results;
    }

    private static List<string> MergeContinuationLines(List<string> lines)
    {
        if (lines.Count == 0)
        {
            return lines;
        }

        var merged = new List<string> { lines[0] };

        for (var i = 1; i < lines.Count; i++)
        {
            var current = lines[i];
            var previous = merged[^1];

            if (ShouldMergeWithPrevious(previous, current))
            {
                merged[^1] = JoinLines(previous, current);
            }
            else
            {
                merged.Add(current);
            }
        }

        return merged;
    }

    private static bool ShouldMergeWithPrevious(string previous, string next)
    {
        if (previous.EndsWith('-'))
        {
            return true;
        }

        if (previous.EndsWith(',') && next.Length <= 30)
        {
            return true;
        }

        var prevLower = previous.ToLowerInvariant();
        var nextLower = next.ToLowerInvariant();

        if (prevLower.Contains("sun", StringComparison.Ordinal)
            && nextLower.StartsWith("dried", StringComparison.Ordinal))
        {
            return true;
        }

        if (prevLower.Contains("chicken", StringComparison.Ordinal)
            && !prevLower.Contains("breast", StringComparison.Ordinal)
            && nextLower.Contains("breast", StringComparison.Ordinal))
        {
            return true;
        }

        if (prevLower.Contains("chicken", StringComparison.Ordinal)
            && !prevLower.Contains("broth", StringComparison.Ordinal)
            && nextLower.Contains("broth", StringComparison.Ordinal))
        {
            return true;
        }

        if (HasQuantity(previous)
            && !previous.Contains(',', StringComparison.Ordinal)
            && next.Length <= 20
            && FragmentWords.Contains(next.Split(' ', 2)[0]))
        {
            return true;
        }

        return false;
    }

    private static string JoinLines(string previous, string next)
    {
        if (previous.EndsWith('-'))
        {
            return $"{previous[..^1]}-{next}";
        }

        if (previous.EndsWith(','))
        {
            return $"{previous} {next}";
        }

        return $"{previous} {next}";
    }

    private static bool HasQuantity(string line) =>
        QuantityLinePattern().IsMatch(line) || char.IsDigit(line[0]);

    private static string NormalizeLine(string line)
    {
        line = line.Trim();
        line = line.Replace("**", string.Empty, StringComparison.Ordinal);
        line = BulletPrefixPattern().Replace(line, string.Empty);
        line = DigitLetterPattern().Replace(line, "$1 $2");
        line = Regex.Replace(line, @"\s{2,}", " ");
        return line.Trim();
    }

    private static bool ShouldSkip(string line)
    {
        var lower = line.ToLowerInvariant();
        return SkipLinePrefixes.Any(prefix => lower.StartsWith(prefix, StringComparison.Ordinal))
            || (!char.IsLetter(lower[0]) && !char.IsDigit(lower[0]));
    }

    private static bool IsGarbage(string line)
    {
        if (GarbagePattern().IsMatch(line))
        {
            return true;
        }

        var letters = line.Count(char.IsLetter);
        return letters < line.Length * 0.35;
    }

    private static bool IsLikelyFragment(string name, string quantity)
    {
        if (!string.IsNullOrWhiteSpace(quantity))
        {
            return false;
        }

        if (name.Contains(' '))
        {
            return false;
        }

        return FragmentWords.Contains(name)
            || name.Length < 5;
    }

    private static (string Name, string Quantity) ExtractNameAndQuantity(string line)
    {
        var match = QuantityLinePattern().Match(line);
        if (!match.Success)
        {
            return (line, string.Empty);
        }

        var qtyPart = match.Groups[1].Value.Trim();
        var unitPart = match.Groups[2].Value.Trim();
        var name = match.Groups[3].Value.Trim();

        var quantity = string.IsNullOrEmpty(unitPart) ? qtyPart : $"{qtyPart} {unitPart}".Trim();
        return (name, quantity);
    }

    private static string CleanIngredientName(string name)
    {
        name = name.Trim().TrimEnd(',');
        if (name.Length == 0)
        {
            return name;
        }

        return char.ToUpper(name[0]) + name[1..];
    }

    private static string CleanQuantity(string quantity)
    {
        quantity = quantity.Trim();
        if (quantity.Length == 0)
        {
            return string.Empty;
        }

        return quantity;
    }

    private static string InferCategory(string name)
    {
        foreach (var (pattern, category) in CategoryRules)
        {
            if (pattern.IsMatch(name))
            {
                return category;
            }
        }

        return "Other";
    }
}
