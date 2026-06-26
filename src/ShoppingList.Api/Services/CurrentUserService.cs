using ShoppingList.Api.Persistence;

namespace ShoppingList.Api.Services;

/// <summary>
/// Resolves the active user until full authentication is implemented.
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor)
{
    public Guid GetUserId()
    {
        var context = httpContextAccessor.HttpContext;
        if (context?.Request.Headers.TryGetValue("X-User-Id", out var header) == true
            && Guid.TryParse(header.ToString(), out var fromHeader))
        {
            return fromHeader;
        }

        return DevelopmentDataSeeder.DemoUserId;
    }
}
