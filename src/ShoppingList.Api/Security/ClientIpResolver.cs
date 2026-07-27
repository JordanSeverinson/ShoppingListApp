namespace ShoppingList.Api.Security;

public static class ClientIpResolver
{
    /// <summary>
    /// Uses the connection remote address after Forwarded Headers middleware has
    /// rewritten it from trusted proxies only. Never reads raw X-Forwarded-For.
    /// </summary>
    public static string GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
