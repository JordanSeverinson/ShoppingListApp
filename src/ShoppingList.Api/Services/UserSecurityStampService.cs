using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ShoppingList.Domain.Entities;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class UserSecurityStampService(ApplicationDbContext db, IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static string CacheKey(Guid userId) => $"user-security-stamp:{userId}";

    public async Task<Guid> GetStampAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey(userId), out Guid cachedStamp))
        {
            return cachedStamp;
        }

        var stamp = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.SecurityStamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (stamp == Guid.Empty)
        {
            return Guid.Empty;
        }

        cache.Set(CacheKey(userId), stamp, CacheDuration);
        return stamp;
    }

    public Guid RotateStamp(User user)
    {
        user.SecurityStamp = Guid.NewGuid();
        cache.Remove(CacheKey(user.Id));
        return user.SecurityStamp;
    }

    public void InvalidateCache(Guid userId) =>
        cache.Remove(CacheKey(userId));
}
