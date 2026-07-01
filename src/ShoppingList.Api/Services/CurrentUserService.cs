using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ShoppingList.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor)
{
    public Guid? TryGetUserId()
    {
        var context = httpContextAccessor.HttpContext;
        if (context?.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var subject = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }

    public Guid GetUserId() =>
        TryGetUserId()
        ?? throw new UnauthorizedAccessException("Authentication required.");
}
