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
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(expiryMinutes),
                Path = "/",
            });
    }

    public void ClearAuthCookie(HttpResponse response)
    {
        response.Cookies.Delete(
            AuthConstants.CookieName,
            new CookieOptions
            {
                Path = "/",
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Lax,
            });
    }
}
