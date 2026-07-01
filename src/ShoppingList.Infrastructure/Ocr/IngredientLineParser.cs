using System.Text;
using System.Text.RegularExpressions;
using ShoppingList.Application.Parsing;

namespace ShoppingList.Infrastructure.Ocr;

/// <summary>
/// Converts raw OCR text into structured recipe content (ingredients with optional sections and steps).
/// </summary>
internal static partial class IngredientLineParser
{
    private static readonly string[] SkipLinePrefixes =
    [
        "ingredients", "directions", "instructions", "method", "preparation",
        "serves", "yield", "nutrition", "recipe", "notes", "optional"
    ];

    private static readonly string[] IngredientUnitWords =
    [
        "cup", "cups", "tbsp", "tsp", "teaspoon", "teaspoons", "tablespoon", "tablespoons",
        "oz", "ounce", "ounces", "lb", "lbs", "pound", "pounds", "gram", "grams", "kg",
        "ml", "liter", "liters", "can", "cans", "clove", "cloves", "slice", "slices",
        "piece", "pieces", "package", "packages", "pinch", "dash", "head", "bunch",
        "stalk", "stalks", "stick", "sticks", "large", "small", "medium"
    ];

    private static readonly HashSet<string> CommonIngredientWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "salt", "pepper", "butter", "water", "milk", "flour", "sugar", "eggs", "oil", "cream",
        "cheese", "garlic", "onion", "honey", "vinegar", "basil", "thyme", "parsley", "cilantro"
    };

    private static readonly HashSet<string> ContinuationWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "needed", "optional", "divided", "softened", "melted", "chopped", "minced", "diced",
        "drained", "thawed", "sifted", "packed", "trimmed", "peeled", "seeded", "serving"
    };

    private static readonly HashSet<string> KnownSectionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cake", "glaze", "frosting", "icing", "dough", "filling", "sauce", "topping",
        "crust", "batter", "marinade", "dressing", "ganache", "assembly"
    };

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

    [GeneratedRegex(@"^[\-\*\u2022\u2023\u25E6\u2043\u25A1\u2610\u2611\u2612\u25CF\u25CB\u25AA\u25AB\[\]\(\)□■▪●○◦@]+[\.\)\:]?\s*", RegexOptions.Compiled)]
    private static partial Regex BulletPrefixPattern();

    [GeneratedRegex(@"\s+[\-\*\u2022\u2023\u25E6\u2043\u25A1\u25CF\u25CB\u25AA\u25AB□■▪●○◦@]+\s+", RegexOptions.Compiled)]
    private static partial Regex InlineBulletSeparatorPattern();

    [GeneratedRegex(@"(\d)([A-Za-z])", RegexOptions.Compiled)]
    private static partial Regex DigitLetterPattern();

    [GeneratedRegex(@"^(\d+\s*/\s*\d+|\d+(?:\.\d+)?)\s*X\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ServingMultiplierPattern();

    [GeneratedRegex(@"^(\d+)[\.\)]\s*(.+)$", RegexOptions.Compiled)]
    private static partial Regex NumberedStepPattern();

    [GeneratedRegex(@"^(?<!\d)(\d{1,2})\s+([A-Za-z].+)$", RegexOptions.Compiled)]
    private static partial Regex BareNumberedStepPattern();

    [GeneratedRegex(@"^(?<!\d)(\d{1,2})$", RegexOptions.Compiled)]
    private static partial Regex StandaloneStepNumberPattern();

    [GeneratedRegex(@"^Step\s+\d+\s*:?\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex StepHeaderPattern();

    [GeneratedRegex(@"(?<=\S)\s+(?=Step\s+\d+\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmbeddedStepSplitPattern();

    [GeneratedRegex(@"(?<=\.)\s+(?=\d+\.\s)", RegexOptions.Compiled)]
    private static partial Regex EmbeddedNumberedStepSplitPattern();

    [GeneratedRegex(@"(?<=[.!?])\s+(?=\d{1,2}\s+)", RegexOptions.Compiled)]
    private static partial Regex EmbeddedBareNumberStepSplitPattern();

    [GeneratedRegex(@"(\d+\s*/\s*\d+|\d+(?:\.\d+)?)(?:\s*(cup|cups|tbsp|tsp|teaspoon|teaspoons|tablespoon|tablespoons|oz|ounce|ounces|lb|lbs|pound|pounds|g|gram|grams|kg|ml|l|liter|liters|can|cans|clove|cloves|slice|slices|piece|pieces|package|packages|pinch|dash|head|bunch|stalk|stalks|stick|sticks|large|small|medium))?\.?\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
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

    [GeneratedRegex(@"\b(salt|pepper|black pepper|chili powder|paprika|cumin|cinnamon|nutmeg|oregano|turmeric|curry powder|garlic powder|onion powder|seasoning|spice|spices|cayenne|vanilla|ginger powder|italian seasoning|red pepper flakes|kosher salt|lemon extract|lemon zest|baking soda)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SeasoningsPattern();

    [GeneratedRegex(@"\b(flour|sugar|oil|vinegar|rice|pasta|beans|broth|stock|sauce|honey|syrup|nuts|oat|parmesan|tomatoes|confectioners)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PantryPattern();

    [GeneratedRegex(@"[0O]{4,}|[^a-zA-Z0-9\s,\.\-/']{5,}", RegexOptions.Compiled)]
    private static partial Regex GarbagePattern();

    public static IReadOnlyList<ParsedIngredientDto> Parse(string ocrText) =>
        ParseRecipeContent(ocrText).Ingredients;

    public static ParsedRecipeContentDto ParseRecipeContent(
        string ocrText,
        RecipeImageImportMode importMode = RecipeImageImportMode.FullRecipeWithSteps)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
        {
            return new ParsedRecipeContentDto([], []);
        }

        if (importMode == RecipeImageImportMode.CookingStepsOnly)
        {
            return ParseStepsOnlyContent(ocrText);
        }

        if (importMode == RecipeImageImportMode.IngredientsOnly)
        {
            return ParseIngredientsOnlyContent(ocrText);
        }

        return ParseFullRecipeContent(ocrText);
    }

    private static ParsedRecipeContentDto ParseFullRecipeContent(string ocrText)
    {
        var rawLines = ExpandToIngredientLines(ocrText);
        var ingredientLines = new List<string>();
        var directionLines = new List<string>();
        var inDirections = false;
        var pastIngredientsHeader = false;

        foreach (var line in rawLines)
        {
            if (IsDirectionsHeader(line))
            {
                inDirections = true;
                pastIngredientsHeader = true;
                continue;
            }

            if (inDirections)
            {
                if (!IsGarbage(line) && IsSubstantiveDirectionLine(line))
                {
                    directionLines.Add(line);
                }

                continue;
            }

            if (IsIngredientsHeader(line))
            {
                pastIngredientsHeader = true;
                continue;
            }

            if (!pastIngredientsHeader && IsLikelyIngredientsBlockStart(line))
            {
                pastIngredientsHeader = true;
            }

            if (!pastIngredientsHeader)
            {
                continue;
            }

            if (ShouldSkipIngredientMeta(line) || IsGarbage(line) || IsCheckboxOcrArtifact(line))
            {
                continue;
            }

            if (line.Length >= 2)
            {
                ingredientLines.Add(line);
            }
        }

        var ingredients = ParseIngredientLines(ingredientLines);
        var steps = ParseDirectionLines(directionLines);

        return new ParsedRecipeContentDto(ingredients, steps);
    }

    private static ParsedRecipeContentDto ParseIngredientsOnlyContent(string ocrText)
    {
        var rawLines = ExpandToIngredientLines(ocrText);
        var ingredientLines = new List<string>();
        var pastIngredientsHeader = false;

        foreach (var line in rawLines)
        {
            if (IsDirectionsHeader(line))
            {
                break;
            }

            if (IsIngredientsHeader(line))
            {
                pastIngredientsHeader = true;
                continue;
            }

            if (!pastIngredientsHeader && IsLikelyIngredientsBlockStart(line))
            {
                pastIngredientsHeader = true;
            }

            if (!pastIngredientsHeader)
            {
                continue;
            }

            if (ShouldSkipIngredientMeta(line) || IsGarbage(line) || IsCheckboxOcrArtifact(line))
            {
                continue;
            }

            if (line.Length >= 2)
            {
                ingredientLines.Add(line);
            }
        }

        return new ParsedRecipeContentDto(ParseIngredientLines(ingredientLines), []);
    }

    private static ParsedRecipeContentDto ParseStepsOnlyContent(string ocrText)
    {
        var rawLines = ExpandToIngredientLines(ocrText);
        var directionLines = new List<string>();
        var hasIngredientsHeader = rawLines.Any(IsIngredientsHeader);
        var hasDirectionsHeader = rawLines.Any(IsDirectionsHeader);
        var inIngredients = false;
        var inDirections = !hasIngredientsHeader || !hasDirectionsHeader;

        foreach (var line in rawLines)
        {
            if (IsIngredientsHeader(line))
            {
                inIngredients = true;
                inDirections = false;
                continue;
            }

            if (IsDirectionsHeader(line))
            {
                inIngredients = false;
                inDirections = true;
                continue;
            }

            if (inIngredients || (!inDirections && hasIngredientsHeader && hasDirectionsHeader))
            {
                continue;
            }

            if (IsGarbage(line) || !IsSubstantiveDirectionLine(line) || ShouldSkipIngredientMeta(line) || IsCheckboxOcrArtifact(line))
            {
                continue;
            }

            directionLines.Add(line);
        }

        return new ParsedRecipeContentDto([], ParseDirectionLines(directionLines));
    }

    private static List<string> ExpandToIngredientLines(string ocrText)
    {
        var lines = new List<string>();

        foreach (var rawLine in ocrText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var fragment in SplitBulletSeparatedFragments(rawLine))
            {
                var normalized = NormalizeLine(fragment);
                if (normalized.Length >= 1)
                {
                    lines.Add(normalized);
                }
            }
        }

        return lines;
    }

    private static IEnumerable<string> SplitBulletSeparatedFragments(string line)
    {
        var parts = InlineBulletSeparatorPattern().Split(line);
        if (parts.Length <= 1)
        {
            yield return line;
            yield break;
        }

        foreach (var part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                yield return part;
            }
        }
    }

    private static List<ParsedIngredientDto> ParseIngredientLines(List<string> lines)
    {
        var mergedLines = MergeContinuationLines(lines);
        var results = new List<ParsedIngredientDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        foreach (var line in mergedLines)
        {
            if (IsSectionHeader(line))
            {
                currentSection = CleanSectionName(line);
                continue;
            }

            if (ShouldSkip(line) || IsGarbage(line) || IsCheckboxOcrArtifact(line))
            {
                continue;
            }

            var (name, quantity) = ExtractNameAndQuantity(line);
            if (string.IsNullOrWhiteSpace(name) || IsLikelyFragment(name, quantity))
            {
                continue;
            }

            name = CleanIngredientName(name);
            quantity = CleanQuantity(quantity);

            var dedupeKey = $"{currentSection}|{quantity}|{name}".ToLowerInvariant();
            if (!seen.Add(dedupeKey))
            {
                continue;
            }

            results.Add(new ParsedIngredientDto(
                Name: name,
                Quantity: quantity,
                Category: InferCategory(name),
                Section: currentSection));
        }

        return results;
    }

    private static List<string> ParseDirectionLines(List<string> lines)
    {
        if (lines.Count == 0)
        {
            return [];
        }

        var expanded = ExpandDirectionLines(lines);
        var merged = MergeContinuationLines(expanded);
        var steps = new List<string>();
        var current = new StringBuilder();

        foreach (var line in merged)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (TryParseStepLine(trimmed, out var stepText))
            {
                if (current.Length > 0)
                {
                    steps.Add(current.ToString().Trim());
                    current.Clear();
                }

                if (!string.IsNullOrWhiteSpace(stepText))
                {
                    current.Append(stepText);
                }

                continue;
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(trimmed);
        }

        if (current.Length > 0)
        {
            steps.Add(current.ToString().Trim());
        }

        return steps
            .Select(step => step.Trim())
            .Where(step => step.Length >= 3)
            .ToList();
    }

    private static List<string> ExpandDirectionLines(List<string> lines)
    {
        var expanded = new List<string>();

        foreach (var line in lines)
        {
            var fragments = EmbeddedStepSplitPattern().Split(line);
            foreach (var fragment in fragments)
            {
                foreach (var numbered in EmbeddedNumberedStepSplitPattern().Split(fragment))
                {
                    foreach (var bareNumbered in EmbeddedBareNumberStepSplitPattern().Split(numbered))
                    {
                        var trimmed = bareNumbered.Trim();
                        if (IsSubstantiveDirectionLine(trimmed))
                        {
                            expanded.Add(trimmed);
                        }
                    }
                }
            }
        }

        return expanded;
    }

    private static bool TryParseStepLine(string line, out string stepText)
    {
        stepText = string.Empty;
        var trimmed = line.Trim();

        if (StandaloneStepNumberPattern().IsMatch(trimmed))
        {
            return true;
        }

        var stepHeaderMatch = StepHeaderPattern().Match(trimmed);
        if (stepHeaderMatch.Success)
        {
            stepText = stepHeaderMatch.Groups[1].Value.Trim();
            return true;
        }

        var numberedMatch = NumberedStepPattern().Match(trimmed);
        if (numberedMatch.Success)
        {
            stepText = numberedMatch.Groups[2].Value.Trim();
            return true;
        }

        var bareNumberedMatch = BareNumberedStepPattern().Match(trimmed);
        if (bareNumberedMatch.Success)
        {
            stepText = bareNumberedMatch.Groups[2].Value.Trim();
            return true;
        }

        return false;
    }

    private static bool IsStepHeaderLine(string line)
    {
        var trimmed = line.Trim();
        return StandaloneStepNumberPattern().IsMatch(trimmed)
            || StepHeaderPattern().IsMatch(trimmed)
            || NumberedStepPattern().IsMatch(trimmed)
            || BareNumberedStepPattern().IsMatch(trimmed);
    }

    private static bool IsIngredientsHeader(string line) =>
        line.Equals("ingredients", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("ingredients:", StringComparison.OrdinalIgnoreCase);

    private static bool IsDirectionsHeader(string line)
    {
        var lower = line.ToLowerInvariant();
        return lower is "directions" or "instructions" or "method" or "preparation"
            || lower.StartsWith("directions:", StringComparison.Ordinal)
            || lower.StartsWith("instructions:", StringComparison.Ordinal);
    }

    private static bool IsLikelyIngredientsBlockStart(string line) =>
        HasQuantity(line) || IsSectionHeader(line);

    private static bool ShouldSkipIngredientMeta(string line)
    {
        if (ServingMultiplierPattern().IsMatch(line))
        {
            return true;
        }

        var lower = line.ToLowerInvariant();
        if (lower.Contains("yields", StringComparison.Ordinal) && lower.Contains("serving", StringComparison.Ordinal))
        {
            return true;
        }

        if (lower.StartsWith("original recipe", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static bool IsSectionHeader(string line)
    {
        if (HasQuantity(line))
        {
            return false;
        }

        if (line.Contains(',', StringComparison.Ordinal))
        {
            return false;
        }

        var lower = line.ToLowerInvariant();
        if (IngredientUnitWords.Any(unit => lower.Contains($"{unit} ", StringComparison.Ordinal)
            || lower.EndsWith($" {unit}", StringComparison.Ordinal)
            || lower.Equals(unit, StringComparison.Ordinal)))
        {
            return false;
        }

        if (SkipLinePrefixes.Any(prefix => lower.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return false;
        }

        if (lower.StartsWith("for the ", StringComparison.Ordinal)
            || lower.StartsWith("for a ", StringComparison.Ordinal)
            || line.EndsWith(':'))
        {
            return true;
        }

        var cleaned = CleanSectionName(line);
        if (cleaned.Length == 0 || cleaned.Length > 24)
        {
            return false;
        }

        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 1)
        {
            return false;
        }

        if (ContinuationWords.Contains(words[0]) || CommonIngredientWords.Contains(words[0]))
        {
            return false;
        }

        if (KnownSectionNames.Contains(cleaned))
        {
            return true;
        }

        return char.IsUpper(cleaned[0]);
    }

    private static string CleanSectionName(string line)
    {
        line = line.Trim().TrimEnd(':');
        var lower = line.ToLowerInvariant();

        if (lower.StartsWith("for the ", StringComparison.Ordinal))
        {
            line = line[8..].Trim();
        }
        else if (lower.StartsWith("for ", StringComparison.Ordinal))
        {
            line = line[4..].Trim();
        }

        if (line.Length == 0)
        {
            return string.Empty;
        }

        return char.ToUpper(line[0]) + line[1..];
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
        if (IsStepHeaderLine(next) || IsStandaloneStepNumber(previous))
        {
            return false;
        }

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

        if (ShouldMergeIngredientNoteContinuation(previous, next))
        {
            return true;
        }

        return false;
    }

    private static bool ShouldMergeIngredientNoteContinuation(string previous, string next)
    {
        if (!HasQuantity(previous) || HasQuantity(next))
        {
            return false;
        }

        if (IsSectionHeader(next) || IsDirectionsHeader(next) || ShouldSkipIngredientMeta(next))
        {
            return false;
        }

        var nextWords = next.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nextWords.Length is < 1 or > 4 || next.Length > 35)
        {
            return false;
        }

        if (EndsWithIncompletePhrase(previous))
        {
            return true;
        }

        return nextWords.Length == 1
            && !CommonIngredientWords.Contains(nextWords[0]);
    }

    private static bool EndsWithIncompletePhrase(string line)
    {
        var lower = line.ToLowerInvariant();
        return lower.EndsWith(" as", StringComparison.Ordinal)
            || lower.EndsWith(" to", StringComparison.Ordinal)
            || lower.EndsWith(" or", StringComparison.Ordinal)
            || lower.EndsWith(" for", StringComparison.Ordinal)
            || lower.EndsWith(" and", StringComparison.Ordinal)
            || lower.EndsWith(" with", StringComparison.Ordinal)
            || lower.EndsWith(" plus", StringComparison.Ordinal)
            || lower.EndsWith(" more", StringComparison.Ordinal);
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
        line = line.Replace("½", "1/2", StringComparison.Ordinal)
            .Replace("¼", "1/4", StringComparison.Ordinal)
            .Replace("¾", "3/4", StringComparison.Ordinal)
            .Replace("⅓", "1/3", StringComparison.Ordinal)
            .Replace("⅔", "2/3", StringComparison.Ordinal);
        line = BulletPrefixPattern().Replace(line, string.Empty);
        line = FixLeadingQuantityOcrErrors(line);
        line = DigitLetterPattern().Replace(line, "$1 $2");
        line = Regex.Replace(line, @"\s{2,}", " ");
        return line.Trim();
    }

    private static string FixLeadingQuantityOcrErrors(string line)
    {
        if (line.Length < 2)
        {
            return line;
        }

        if (line[0] is 'l' or 'I' or '|' && (char.IsWhiteSpace(line[1]) || char.IsDigit(line[1])))
        {
            return $"1{line[1..]}";
        }

        if (line[0] == 'O' && char.IsWhiteSpace(line[1]))
        {
            return $"0{line[1..]}";
        }

        return line;
    }

    private static bool ShouldSkip(string line)
    {
        var lower = line.ToLowerInvariant();
        return SkipLinePrefixes.Any(prefix => lower.StartsWith(prefix, StringComparison.Ordinal))
            || (!char.IsLetter(lower[0]) && !char.IsDigit(lower[0]));
    }

    private static bool IsGarbage(string line)
    {
        if (IsStandaloneStepNumber(line))
        {
            return false;
        }

        if (IsCheckboxOcrArtifact(line))
        {
            return true;
        }

        if (GarbagePattern().IsMatch(line))
        {
            return true;
        }

        var letters = line.Count(char.IsLetter);
        return letters < line.Length * 0.35;
    }

    private static bool IsStandaloneStepNumber(string line) =>
        StandaloneStepNumberPattern().IsMatch(line.Trim());

    private static bool IsCheckboxOcrArtifact(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length < 3)
        {
            return false;
        }

        if (QuantityLinePattern().Match(trimmed) is { Success: true } quantityMatch)
        {
            var namePart = quantityMatch.Groups[3].Value.Trim();
            if (namePart.Length > 0 && !IsOcrNoiseName(namePart))
            {
                return false;
            }
        }
        else if (char.IsDigit(trimmed[0]) && trimmed.Any(char.IsLetter) && !IsMostlyOcrZeroNoise(trimmed))
        {
            return false;
        }

        var nonSpace = trimmed.Where(c => !char.IsWhiteSpace(c)).ToArray();
        if (nonSpace.Length == 0)
        {
            return true;
        }

        var oZeroCount = nonSpace.Count(c => c is '0' or 'O' or 'o');
        if (oZeroCount < 2 || oZeroCount < nonSpace.Length * 0.35)
        {
            return false;
        }

        var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var substantiveWords = words.Count(word =>
            word.Length >= 4
            && !word.All(c => c is '0' or 'O' or 'o')
            && CommonIngredientWords.Contains(word) is false
            && !IngredientUnitWords.Contains(word, StringComparer.OrdinalIgnoreCase));

        if (substantiveWords > 0)
        {
            return false;
        }

        var hasIngredientSignal = CommonIngredientWords.Any(word =>
                trimmed.Contains(word, StringComparison.OrdinalIgnoreCase))
            || CategoryRules.Any(rule => rule.Pattern.IsMatch(trimmed));

        return !hasIngredientSignal;
    }

    private static bool IsMostlyOcrZeroNoise(string line)
    {
        var nonSpace = line.Where(c => !char.IsWhiteSpace(c)).ToArray();
        if (nonSpace.Length == 0)
        {
            return true;
        }

        var oZeroCount = nonSpace.Count(c => c is '0' or 'O' or 'o');
        return oZeroCount >= 2 && oZeroCount >= nonSpace.Length * 0.35;
    }

    private static bool IsOcrNoiseName(string name) =>
        IsMostlyOcrZeroNoise(name) && !CategoryRules.Any(rule => rule.Pattern.IsMatch(name))
        && !CommonIngredientWords.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase));

    private static bool IsSubstantiveDirectionLine(string line) =>
        line.Length >= 2 || IsStandaloneStepNumber(line);

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
