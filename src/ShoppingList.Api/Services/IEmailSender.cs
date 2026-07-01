namespace ShoppingList.Api.Services;

public interface IEmailSender
{
    Task SendVerificationEmailAsync(
        string email,
        string preferredName,
        string verificationUrl,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetEmailAsync(
        string email,
        string preferredName,
        string resetUrl,
        CancellationToken cancellationToken = default);
}
