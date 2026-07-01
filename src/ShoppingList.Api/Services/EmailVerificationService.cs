using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class EmailVerificationService(
    ApplicationDbContext db,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<EmailVerificationService> logger)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    public async Task IssueVerificationEmailAsync(
        Domain.Entities.User user,
        CancellationToken cancellationToken = default)
    {
        user.EmailVerificationToken = GenerateToken();
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.Add(TokenLifetime);
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var frontendBaseUrl = configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173";
        var verificationUrl =
            $"{frontendBaseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(user.EmailVerificationToken)}";

        await emailSender.SendVerificationEmailAsync(
            user.Email,
            user.PreferredName ?? user.FirstName,
            verificationUrl,
            cancellationToken);

        logger.LogInformation("Verification email queued for {Email}", user.Email);
    }

    public async Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.EmailVerificationToken == token,
            cancellationToken);

        if (user is null)
        {
            return false;
        }

        if (user.EmailVerificationTokenExpiresAt is null
            || user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;
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
