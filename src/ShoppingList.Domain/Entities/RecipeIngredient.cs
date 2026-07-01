using ShoppingList.Domain.Common;

namespace ShoppingList.Domain.Entities;

public class RecipeIngredient : AuditableEntity
{
    public Guid RecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string Category { get; set; } = "Other";
    public string? Section { get; set; }
    public int SortOrder { get; set; }

    public Recipe Recipe { get; set; } = null!;
}
