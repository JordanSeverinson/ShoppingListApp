namespace ShoppingList.Api.Services;

public class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendVerificationEmailAsync(
        string email,
        string preferredName,
        string verificationUrl,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            """
            [DEV EMAIL] Verification email for {Email} ({PreferredName})
            Subject: Verify your Cook With Me account
            Link: {VerificationUrl}
            """,
            email,
            preferredName,
            RedactVerificationUrl(verificationUrl));

        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(
        string email,
        string preferredName,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            """
            [DEV EMAIL] Password reset email for {Email} ({PreferredName})
            Subject: Reset your Cook With Me password
            Link: {ResetUrl}
            """,
            email,
            preferredName,
            RedactAuthUrl(resetUrl));

        return Task.CompletedTask;
    }

    private static string RedactAuthUrl(string url) =>
        RedactVerificationUrl(url);

    private static string RedactVerificationUrl(string verificationUrl)
    {
        if (!Uri.TryCreate(verificationUrl, UriKind.Absolute, out var uri))
        {
            return "[redacted]";
        }

        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        if (query.TryGetValue("token", out var queryToken) && !string.IsNullOrEmpty(queryToken))
        {
            return RedactToken(uri.GetLeftPart(UriPartial.Path) + "?token=", queryToken.ToString());
        }

        var fragment = uri.Fragment.TrimStart('#');
        var hashParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(fragment);
        if (hashParams.TryGetValue("token", out var hashToken) && !string.IsNullOrEmpty(hashToken))
        {
            return RedactToken(uri.GetLeftPart(UriPartial.Path) + "#token=", hashToken.ToString());
        }

        return verificationUrl;
    }

    private static string RedactToken(string prefix, string token) =>
        prefix + (token.Length > 8 ? $"{token[..4]}…{token[^4..]}" : "…");
}
