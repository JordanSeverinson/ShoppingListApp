using ShoppingList.Domain.Common;

using ShoppingList.Domain.Recipes;

namespace ShoppingList.Domain.Entities;

public class Recipe : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public RecipeContentDocument Content { get; set; } = new();

    public User Owner { get; set; } = null!;
    public ICollection<RecipeIngredient> Ingredients { get; set; } = [];
    public ICollection<RecipeStep> Steps { get; set; } = [];
    public ICollection<RecipeSharedPermission> SharedPermissions { get; set; } = [];
}
