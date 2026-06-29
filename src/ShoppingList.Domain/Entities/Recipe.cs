using ShoppingList.Domain.Common;

namespace ShoppingList.Domain.Entities;

public class Recipe : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string ShareCode { get; set; } = string.Empty;

    public User Owner { get; set; } = null!;
    public ICollection<RecipeIngredient> Ingredients { get; set; } = [];
    public ICollection<RecipeSharedPermission> SharedPermissions { get; set; } = [];
}
