namespace ShoppingList.Domain.Entities;

/// <summary>
/// Persisted hash of a revoked JWT so logout works across API instances.
/// </summary>
public class RevokedJwt
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
