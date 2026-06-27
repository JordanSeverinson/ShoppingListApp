namespace ShoppingList.Application.Parsing;

public interface IIngredientParserService
{
    Task<IReadOnlyList<ParsedIngredientDto>> ParseFromStreamAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default);
}
