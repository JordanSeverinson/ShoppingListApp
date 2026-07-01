using System.Text.Json;
using System.Text.Json.Serialization;
using ShoppingList.Domain.Recipes;

namespace ShoppingList.Application.Recipes;

public sealed class RecipeContentRootJsonConverter : JsonConverter<RecipeContentRoot>
{
    public override RecipeContentRoot Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of recipe content object.");
        }

        var root = new RecipeContentRoot();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return root;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var propertyName = reader.GetString() ?? string.Empty;
            reader.Read();

            if (propertyName.Equals("cookingSteps", StringComparison.OrdinalIgnoreCase))
            {
                root.CookingSteps = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
                continue;
            }

            if (propertyName.StartsWith("subCategory", StringComparison.OrdinalIgnoreCase))
            {
                var block = JsonSerializer.Deserialize<RecipeSubCategoryBlock>(ref reader, options);
                if (block is not null)
                {
                    root.SubCategories.Add(block);
                }
            }
        }

        throw new JsonException("Unexpected end of recipe content JSON.");
    }

    public override void Write(Utf8JsonWriter writer, RecipeContentRoot value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        for (var index = 0; index < value.SubCategories.Count; index++)
        {
            writer.WritePropertyName($"subCategory{index + 1}");
            JsonSerializer.Serialize(writer, value.SubCategories[index], options);
        }

        writer.WritePropertyName("cookingSteps");
        JsonSerializer.Serialize(writer, value.CookingSteps, options);

        writer.WriteEndObject();
    }
}
