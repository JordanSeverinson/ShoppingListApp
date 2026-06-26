using ShoppingList.Domain.Common;

namespace ShoppingList.Domain.Entities;

public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }

    public ICollection<ShoppingList> OwnedLists { get; set; } = [];
    public ICollection<SharedPermission> SharedPermissions { get; set; } = [];
}
