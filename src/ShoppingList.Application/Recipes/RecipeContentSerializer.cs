using System.Text.Json;
using System.Text.Json.Serialization;
using ShoppingList.Domain.Recipes;

namespace ShoppingList.Application.Recipes;

public static class RecipeContentSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new RecipeContentRootJsonConverter() }
    };

    public static string Serialize(RecipeContentDocument content) =>
        JsonSerializer.Serialize(content, Options);

    public static RecipeContentDocument Deserialize(string json) =>
        JsonSerializer.Deserialize<RecipeContentDocument>(json, Options) ?? new RecipeContentDocument();
}
