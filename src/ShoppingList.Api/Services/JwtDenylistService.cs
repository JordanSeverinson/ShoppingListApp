using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ShoppingList.Api.Security;
using ShoppingList.Domain.Entities;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class JwtDenylistService(ApplicationDbContext db, IMemoryCache cache)
{
    private const string CachePrefix = "jwt-deny:";

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
        {
            return;
        }

        var jwt = handler.ReadJwtToken(token);
        var remaining = jwt.ValidTo - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return;
        }

        var hash = TokenHasher.Hash(token);
        var exists = await db.RevokedJwts
            .AnyAsync(r => r.TokenHash == hash, cancellationToken);
        if (!exists)
        {
            db.RevokedJwts.Add(new RevokedJwt
            {
                Id = Guid.NewGuid(),
                TokenHash = hash,
                ExpiresAt = jwt.ValidTo.ToUniversalTime(),
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        cache.Set(CachePrefix + hash, true, remaining);
    }

    public async Task<bool> IsRevokedAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var hash = TokenHasher.Hash(token);
        if (cache.TryGetValue(CachePrefix + hash, out _))
        {
            return true;
        }

        var now = DateTime.UtcNow;
        var revoked = await db.RevokedJwts
            .AsNoTracking()
            .AnyAsync(r => r.TokenHash == hash && r.ExpiresAt > now, cancellationToken);

        if (revoked)
        {
            cache.Set(CachePrefix + hash, true, TimeSpan.FromMinutes(5));
        }

        return revoked;
    }
}
