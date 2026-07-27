namespace ShoppingList.Api.Security;

/// <summary>
/// Rejects browser requests whose Origin is neither the CORS allowlist nor same-origin.
/// Protects SignalR negotiate/WebSocket and API calls from disallowed sites.
/// </summary>
public class OriginAllowlistMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly HashSet<string> _allowedOrigins = new(
        configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"],
        StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (!string.IsNullOrWhiteSpace(origin)
            && !_allowedOrigins.Contains(origin)
            && !IsSameOrigin(context.Request, origin))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Origin is not allowed." });
            return;
        }

        await next(context);
    }

    private static bool IsSameOrigin(HttpRequest request, string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return false;
        }

        if (!string.Equals(originUri.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(originUri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var originPort = originUri.IsDefaultPort ? DefaultPort(originUri.Scheme) : originUri.Port;
        var requestPort = request.Host.Port ?? DefaultPort(request.Scheme);
        return originPort == requestPort;
    }

    private static int DefaultPort(string scheme) =>
        string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80;
}
