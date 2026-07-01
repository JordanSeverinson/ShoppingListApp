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
            verificationUrl);

        return Task.CompletedTask;
    }
}
