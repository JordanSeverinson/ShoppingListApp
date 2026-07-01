using ShoppingList.Domain.Common;

namespace ShoppingList.Domain.Entities;

public class RecipeStep : AuditableEntity
{
    public Guid RecipeId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Recipe Recipe { get; set; } = null!;
}
