using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Security;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class PasswordResetService(
    ApplicationDbContext db,
    IEmailSender emailSender,
    PasswordService passwords,
    IConfiguration configuration,
    ILogger<PasswordResetService> logger)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public const string ForgotPasswordMessage =
        "If an account exists for that email, a password reset link has been sent.";

    public async Task RequestResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (user is null || !user.EmailVerified)
        {
            return;
        }

        var token = GenerateToken();
        user.PasswordResetToken = TokenHasher.Hash(token);
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.Add(TokenLifetime);
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var frontendBaseUrl = configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173";
        var resetUrl =
            $"{frontendBaseUrl.TrimEnd('/')}/reset-password#token={Uri.EscapeDataString(token)}";

        await emailSender.SendPasswordResetEmailAsync(
            user.Email,
            user.PreferredName ?? user.FirstName,
            resetUrl,
            cancellationToken);

        logger.LogInformation("Password reset email queued for {Email}", user.Email);
    }

    public async Task<bool> ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var tokenHash = TokenHasher.Hash(token.Trim());
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.PasswordResetToken == tokenHash,
            cancellationToken);

        if (user is null)
        {
            return false;
        }

        if (user.PasswordResetTokenExpiresAt is null
            || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        user.PasswordHash = passwords.Hash(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
