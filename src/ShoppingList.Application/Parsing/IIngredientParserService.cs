namespace ShoppingList.Application.Parsing;

public interface IIngredientParserService
{
    Task<ParsedRecipeContentDto> ParseFromStreamAsync(
        Stream imageStream,
        RecipeImageImportMode importMode = RecipeImageImportMode.FullRecipeWithSteps,
        CancellationToken cancellationToken = default);
}
