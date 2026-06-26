using ShoppingList.Domain.Common;
using ShoppingList.Domain.Enums;

namespace ShoppingList.Domain.Entities;

/// <summary>
/// Join entity linking users to shared lists (many-to-many with payload).
/// </summary>
public class SharedPermission : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid ShoppingListId { get; set; }
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.Edit;
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ShoppingList ShoppingList { get; set; } = null!;
}
