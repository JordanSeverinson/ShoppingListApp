namespace ShoppingList.Application.Parsing;

public interface IIngredientParserService
{
    Task<IReadOnlyList<ParsedIngredientDto>> ParseFromStreamAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParsedIngredientDto>> ParseFromBase64Async(
        string base64Image,
        CancellationToken cancellationToken = default);
}
