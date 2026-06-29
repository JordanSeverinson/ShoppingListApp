using ShoppingList.Domain.Common;
using ShoppingList.Domain.Enums;

namespace ShoppingList.Domain.Entities;

public class RecipeSharedPermission : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid RecipeId { get; set; }
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.Edit;
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
