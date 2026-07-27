namespace ShoppingList.Api.Security;

/// <summary>
/// Double-submit CSRF: cookie (csrf_token) must match X-CSRF header on mutating
/// cookie-authenticated requests, including SignalR negotiate under /hubs.
/// </summary>
public class CookieCsrfMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> UnsafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresCsrfValidation(context) && !HasValidCsrf(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing CSRF token." });
            return;
        }

        await next(context);
    }

    private static bool RequiresCsrfValidation(HttpContext context)
    {
        if (!UnsafeMethods.Contains(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!context.Request.Cookies.ContainsKey(AuthConstants.CookieName))
        {
            return false;
        }

        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool HasValidCsrf(HttpRequest request)
    {
        var header = request.Headers[AuthConstants.CsrfHeaderName].ToString();
        request.Cookies.TryGetValue(AuthConstants.CsrfCookieName, out var cookie);
        return CsrfTokenService.IsValid(header, cookie);
    }
}
