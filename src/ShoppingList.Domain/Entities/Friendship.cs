using ShoppingList.Domain.Common;
using ShoppingList.Domain.Enums;

namespace ShoppingList.Domain.Entities;

public class Friendship : AuditableEntity
{
    public Guid RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    public Guid AddresseeId { get; set; }
    public User Addressee { get; set; } = null!;

    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
}
