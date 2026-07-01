namespace ShoppingList.Infrastructure.Ocr;

internal static class TesseractPathResolver
{
    public static string Resolve(string configuredPath)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            candidates.Add(Path.GetFullPath(configuredPath));
        }

        candidates.Add(Path.Combine(AppContext.BaseDirectory, "tessdata"));

        // Repo layout when running via `dotnet run` from src/ShoppingList.Api
        var repoTessdata = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "tessdata"));
        candidates.Add(repoTessdata);

        candidates.Add(Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "tessdata")));

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(candidate)
                && File.Exists(Path.Combine(candidate, "eng.traineddata")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Tesseract language data not found. Run .\\scripts\\download-tessdata.ps1 from the repo root, " +
            "or set Tesseract:DataPath in appsettings.");
    }
}
