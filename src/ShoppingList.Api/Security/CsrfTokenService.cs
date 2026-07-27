using System.Security.Cryptography;

namespace ShoppingList.Api.Security;

public class CsrfTokenService(IHostEnvironment environment, IConfiguration configuration)
{
    public string IssueToken(HttpResponse response)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(AuthConstants.CsrfTokenBytes));
        WriteCookie(response, token);
        return token;
    }

    public void ClearToken(HttpResponse response)
    {
        response.Cookies.Delete(
            AuthConstants.CsrfCookieName,
            CreateCookieOptions(maxAge: TimeSpan.Zero));
    }

    public static bool IsValid(string? headerToken, string? cookieToken)
    {
        if (string.IsNullOrWhiteSpace(headerToken) || string.IsNullOrWhiteSpace(cookieToken))
        {
            return false;
        }

        var headerBytes = System.Text.Encoding.UTF8.GetBytes(headerToken);
        var cookieBytes = System.Text.Encoding.UTF8.GetBytes(cookieToken);
        return headerBytes.Length == cookieBytes.Length
            && CryptographicOperations.FixedTimeEquals(headerBytes, cookieBytes);
    }

    private void WriteCookie(HttpResponse response, string token)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var minutes)
            ? minutes
            : AuthConstants.DefaultExpiryMinutes;

        response.Cookies.Append(
            AuthConstants.CsrfCookieName,
            token,
            CreateCookieOptions(TimeSpan.FromMinutes(expiryMinutes)));
    }

    private CookieOptions CreateCookieOptions(TimeSpan maxAge) =>
        new()
        {
            // Readable by the SPA so it can mirror the value into X-CSRF (double-submit).
            HttpOnly = false,
            Secure = !environment.IsDevelopment(),
            SameSite = environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
            MaxAge = maxAge,
            Path = "/",
        };
}
