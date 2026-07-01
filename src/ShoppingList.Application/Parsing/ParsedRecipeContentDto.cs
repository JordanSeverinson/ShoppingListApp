namespace ShoppingList.Application.Parsing;

public record ParsedRecipeContentDto(
    IReadOnlyList<ParsedIngredientDto> Ingredients,
    IReadOnlyList<string> Steps);
