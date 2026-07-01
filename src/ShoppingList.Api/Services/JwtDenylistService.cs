using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Memory;
using ShoppingList.Api.Security;

namespace ShoppingList.Api.Services;

public class JwtDenylistService(IMemoryCache cache)
{
    private const string CachePrefix = "jwt-deny:";

    public void Revoke(string token)
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

        cache.Set(CachePrefix + TokenHasher.Hash(token), true, remaining);
    }

    public bool IsRevoked(string token) =>
        !string.IsNullOrWhiteSpace(token)
        && cache.TryGetValue(CachePrefix + TokenHasher.Hash(token), out _);
}
