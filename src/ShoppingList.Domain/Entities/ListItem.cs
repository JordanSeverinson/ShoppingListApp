using ShoppingList.Domain.Common;

namespace ShoppingList.Domain.Entities;

public class ListItem : AuditableEntity
{
    public Guid ShoppingListId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string Category { get; set; } = "Other";
    public bool IsChecked { get; set; }
    public int SortOrder { get; set; }

    public ShoppingList ShoppingList { get; set; } = null!;
}
