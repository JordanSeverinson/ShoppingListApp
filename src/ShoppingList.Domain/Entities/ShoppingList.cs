using ShoppingList.Domain.Common;

namespace ShoppingList.Domain.Entities;

public class ShoppingList : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string ShareCode { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<ListItem> Items { get; set; } = [];
    public ICollection<SharedPermission> SharedPermissions { get; set; } = [];
}
