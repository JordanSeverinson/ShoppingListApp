using ShoppingList.Domain.Common;
using ShoppingList.Domain.Enums;

namespace ShoppingList.Domain.Entities;

/// <summary>
/// Join entity linking users to shopping lists they can access (many-to-many with payload).
/// </summary>
public class SharedPermission : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid ShoppingListId { get; set; }
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.Edit;
    public ListShareStatus Status { get; set; } = ListShareStatus.Pending;
    public Guid InvitedByUserId { get; set; }
    public DateTime? GrantedAt { get; set; }

    public User User { get; set; } = null!;
    public ShoppingList ShoppingList { get; set; } = null!;
}
