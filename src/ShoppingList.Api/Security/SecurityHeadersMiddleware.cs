namespace ShoppingList.Api.Security;

public class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        var path = context.Request.Path.Value ?? string.Empty;
        var isSwagger = environment.IsDevelopment()
            && path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

        // Swagger UI needs scripts/styles; keep a strict CSP everywhere else.
        headers["Content-Security-Policy"] = isSwagger
            ? "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'"
            : "default-src 'none'; frame-ancestors 'none'";

        if (!environment.IsDevelopment())
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        await next(context);
    }
}
