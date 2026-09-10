using System.Net;
using System.Net.Mail;

namespace ShoppingList.Api.Services;

public class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public Task SendVerificationEmailAsync(
        string email,
        string preferredName,
        string verificationUrl,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            email,
            "Verify your Cook In Shop Out account",
            $"""
            Hi {preferredName},

            Please verify your email address by opening this link:

            {verificationUrl}

            This link expires in 24 hours. If you did not create an account, you can ignore this email.
            """,
            "Verification email sent to {Email}",
            cancellationToken);

    public Task SendPasswordResetEmailAsync(
        string email,
        string preferredName,
        string resetUrl,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            email,
            "Reset your Cook In Shop Out password",
            $"""
            Hi {preferredName},

            We received a request to reset your password. Open this link to choose a new password:

            {resetUrl}

            This link expires in 1 hour. If you did not request a reset, you can ignore this email.
            """,
            "Password reset email sent to {Email}",
            cancellationToken);

    private async Task SendAsync(
        string email,
        string subject,
        string body,
        string successLogTemplate,
        CancellationToken cancellationToken)
    {
        var host = configuration["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException(
                "Email:SmtpHost is not configured. Set SMTP settings in appsettings.Production.local.json.");
        }

        var port = int.TryParse(configuration["Email:SmtpPort"], out var parsedPort) ? parsedPort : 587;
        var fromAddress = configuration["Email:FromAddress"]
            ?? throw new InvalidOperationException("Email:FromAddress is not configured.");
        var fromName = configuration["Email:FromName"] ?? "Cook In Shop Out";
        var username = configuration["Email:Username"];
        var password = configuration["Email:Password"];
        var enableSsl = !bool.TryParse(configuration["Email:EnableSsl"], out var ssl) || ssl;

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        message.To.Add(email);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation(successLogTemplate, email);
    }
}
