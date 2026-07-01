using ShoppingList.Domain.Common;
using ShoppingList.Domain.Enums;

namespace ShoppingList.Domain.Entities;

public class RecipeSharedPermission : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid RecipeId { get; set; }
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.Edit;
    public ListShareStatus Status { get; set; } = ListShareStatus.Pending;
    public Guid InvitedByUserId { get; set; }
    public DateTime? GrantedAt { get; set; }

    public User User { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
