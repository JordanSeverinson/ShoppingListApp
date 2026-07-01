using ShoppingList.Domain.Common;

namespace ShoppingList.Domain.Entities;

public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? PreferredName { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string FriendCode { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    public ICollection<ShoppingList> OwnedLists { get; set; } = [];
    public ICollection<SharedPermission> SharedPermissions { get; set; } = [];
    public ICollection<Recipe> OwnedRecipes { get; set; } = [];
    public ICollection<RecipeSharedPermission> RecipeSharedPermissions { get; set; } = [];
    public ICollection<Friendship> SentFriendRequests { get; set; } = [];
    public ICollection<Friendship> ReceivedFriendRequests { get; set; } = [];
}
