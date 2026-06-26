using System.Text.RegularExpressions;
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
    private static readonly Regex DataUrlPrefix = new(
        @"^data:image\/[a-zA-Z+]+;base64,",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Task<IReadOnlyList<ParsedIngredientDto>> ParseFromStreamAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => ParseImageBytes(ReadAllBytes(imageStream), cancellationToken), cancellationToken);

    public Task<IReadOnlyList<ParsedIngredientDto>> ParseFromBase64Async(
        string base64Image,
        CancellationToken cancellationToken = default)
    {
        var payload = DataUrlPrefix.Replace(base64Image.Trim(), string.Empty);
        var bytes = Convert.FromBase64String(payload);
        return Task.Run(() => ParseImageBytes(bytes, cancellationToken), cancellationToken);
    }

    private IReadOnlyList<ParsedIngredientDto> ParseImageBytes(byte[] imageBytes, CancellationToken cancellationToken)
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
            using var page = engine.Process(prepared, PageSegMode.SparseText);
            var text = page.GetText();

            logger.LogDebug("OCR extracted {Length} characters from {DataPath}", text?.Length ?? 0, dataPath);

            return IngredientLineParser.Parse(text ?? string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tesseract OCR failed");
            throw new InvalidOperationException(
                "OCR processing failed. Check that Tesseract native libraries are available and the image is a supported format.",
                ex);
        }

    }

    private static Pix PrepareImage(Pix source)
    {
        const int minWidth = 900;
        if (source.Width >= minWidth)
        {
            return source.Clone();
        }

        var scale = (float)minWidth / source.Width;
        return source.Scale(scale, scale);
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
