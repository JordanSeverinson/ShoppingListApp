using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShoppingList.Application.Parsing;
using Tesseract;

namespace ShoppingList.Infrastructure.Ocr;

public sealed class TesseractOptions
{
    public const string SectionName = "Tesseract";

    public string DataPath { get; set; } = "./tessdata";
    public string Language { get; set; } = "eng";
}

public sealed class TesseractIngredientParserService(
    IOptions<TesseractOptions> options,
    ILogger<TesseractIngredientParserService> logger) : IIngredientParserService
{
    private static readonly PageSegMode[] SegmentationModes =
    [
        PageSegMode.Auto,
        PageSegMode.SingleColumn,
        PageSegMode.SingleBlock,
        PageSegMode.SparseText
    ];

    public Task<ParsedRecipeContentDto> ParseFromStreamAsync(
        Stream imageStream,
        RecipeImageImportMode importMode = RecipeImageImportMode.FullRecipeWithSteps,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => ParseImageBytes(ReadAllBytes(imageStream), importMode, cancellationToken), cancellationToken);

    private ParsedRecipeContentDto ParseImageBytes(
        byte[] imageBytes,
        RecipeImageImportMode importMode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tessOptions = options.Value;
        var dataPath = TesseractPathResolver.Resolve(tessOptions.DataPath);

        try
        {
            using var engine = new TesseractEngine(dataPath, tessOptions.Language, EngineMode.Default);
            engine.SetVariable("preserve_interword_spaces", "1");

            using var pix = Pix.LoadFromMemory(imageBytes);
            using var prepared = PrepareImage(pix);

            ParsedRecipeContentDto? best = null;
            string? bestText = null;
            PageSegMode? bestMode = null;

            foreach (var mode in SegmentationModes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var page = engine.Process(prepared, mode);
                var text = page.GetText() ?? string.Empty;
                var parsed = IngredientLineParser.ParseRecipeContent(text, importMode);

                if (IsBetterOcrResult(parsed, best, importMode))
                {
                    best = parsed;
                    bestText = text;
                    bestMode = mode;
                }
            }

            logger.LogDebug(
                "OCR selected {Mode} with {IngredientCount} ingredients and {StepCount} steps ({Length} chars)",
                bestMode,
                best?.Ingredients.Count ?? 0,
                best?.Steps.Count ?? 0,
                bestText?.Length ?? 0);

            return best ?? new ParsedRecipeContentDto([], []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tesseract OCR failed");
            throw new InvalidOperationException(
                "OCR processing failed. Check that Tesseract native libraries are available and the image is a supported format.",
                ex);
        }
    }

    private static bool IsBetterOcrResult(
        ParsedRecipeContentDto candidate,
        ParsedRecipeContentDto? current,
        RecipeImageImportMode importMode)
    {
        if (current is null)
        {
            return true;
        }

        return importMode switch
        {
            RecipeImageImportMode.IngredientsOnly =>
                candidate.Ingredients.Count > current.Ingredients.Count,
            RecipeImageImportMode.CookingStepsOnly =>
                candidate.Steps.Count > current.Steps.Count,
            _ =>
                candidate.Ingredients.Count > current.Ingredients.Count
                || (candidate.Ingredients.Count == current.Ingredients.Count
                    && candidate.Steps.Count > current.Steps.Count)
        };
    }

    private static Pix PrepareImage(Pix source)
    {
        using var gray = source.Depth == 8 ? source.Clone() : source.ConvertRGBToGray();

        const int minWidth = 1400;
        if (gray.Width >= minWidth)
        {
            return gray.Clone();
        }

        return gray.Scale((float)minWidth / gray.Width, (float)minWidth / gray.Width);
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream is MemoryStream ms && ms.TryGetBuffer(out var segment))
        {
            return segment.ToArray();
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
