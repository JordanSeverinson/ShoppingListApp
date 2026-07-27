namespace ShoppingList.Api.Security;

public class AuthCookieService(IHostEnvironment environment, IConfiguration configuration)
{
    public void SetAuthCookie(HttpResponse response, string token)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var minutes)
            ? minutes
            : AuthConstants.DefaultExpiryMinutes;

        response.Cookies.Append(
            AuthConstants.CookieName,
            token,
            CreateOptions(TimeSpan.FromMinutes(expiryMinutes)));
    }

    public void ClearAuthCookie(HttpResponse response)
    {
        response.Cookies.Delete(
            AuthConstants.CookieName,
            CreateOptions(maxAge: null));
    }

    private CookieOptions CreateOptions(TimeSpan? maxAge) =>
        new()
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
            MaxAge = maxAge,
            Path = "/",
        };
}
