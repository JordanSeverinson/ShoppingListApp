namespace ShoppingList.Api.Services;

public class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendVerificationEmailAsync(
        string email,
        string preferredName,
        string verificationUrl,
        CancellationToken cancellationToken = default)
    {
        // Full link is intentional: this sender only runs in Development and exists so you can open the URL.
        logger.LogWarning(
            """
            [DEV EMAIL] Verification email for {Email} ({PreferredName})
            Subject: Verify your Cook With Me account
            Link: {VerificationUrl}
            """,
            email,
            preferredName,
            verificationUrl);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(
        string email,
        string preferredName,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        // Full link is intentional: this sender only runs in Development and exists so you can open the URL.
        logger.LogWarning(
            """
            [DEV EMAIL] Password reset email for {Email} ({PreferredName})
            Subject: Reset your Cook With Me password
            Link: {ResetUrl}
            """,
            email,
            preferredName,
            resetUrl);

        return Task.CompletedTask;
    }
}
