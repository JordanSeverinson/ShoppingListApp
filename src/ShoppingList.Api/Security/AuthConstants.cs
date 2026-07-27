namespace ShoppingList.Api.Security;

public static class AuthConstants
{
    public const string CookieName = "auth_token";
    public const string CsrfCookieName = "csrf_token";
    public const string SecurityStampClaimType = "sec";
    public const string CsrfHeaderName = "X-CSRF";
    public const int DefaultExpiryMinutes = 120;
    public const int MinJwtKeyBytes = 32;
    public const int CsrfTokenBytes = 32;
}
