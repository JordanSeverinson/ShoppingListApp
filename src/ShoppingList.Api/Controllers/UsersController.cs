using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Api.Security;
using ShoppingList.Api.Services;
using ShoppingList.Application.Users;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(
    ApplicationDbContext db,
    CurrentUserService currentUser,
    FriendCodeAllocationService friendCodes,
    EmailVerificationService emailVerification,
    PasswordService passwords,
    UserSecurityStampService securityStamps,
    JwtTokenService jwtTokens,
    AuthCookieService authCookies,
    ILogger<UsersController> logger) : ControllerBase
{
    private static readonly HashSet<string> AllowedGenders =
        new(StringComparer.OrdinalIgnoreCase) { "Male", "Female", "Non-binary" };

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = currentUser.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized(new { error = "Sign in to view your profile." });
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return NotFound(new { error = "User not found." });
        }

        await EnsureProfileDefaultsAsync(user, cancellationToken);
        return Ok(ToProfile(user));
    }

    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileDto>> UpdateCurrentUser(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized(new { error = "Sign in to update your profile." });
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { error = "User not found." });
        }

        var emailChanged = false;

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailError = RegistrationValidator.ValidateEmail(request.Email);
            if (emailError is not null)
            {
                return BadRequest(new { error = emailError });
            }

            var email = request.Email.Trim().ToLowerInvariant();
            if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await db.Users.AnyAsync(
                    u => u.Id != user.Id && u.Email.ToLower() == email,
                    cancellationToken);

                if (emailTaken)
                {
                    return BadRequest(new { error = ApiErrors.ProfileUpdateFailed });
                }

                user.Email = email;
                user.EmailVerified = false;
                emailChanged = true;
            }
        }

        if (request.PhoneNumber is not null)
        {
            var phone = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim();

            if (phone is not null)
            {
                var phoneError = RegistrationValidator.ValidatePhone(phone);
                if (phoneError is not null)
                {
                    return BadRequest(new { error = phoneError });
                }

                phone = RegistrationValidator.FormatPhone(phone);
                var normalized = PhoneNormalizer.Normalize(phone);
                var otherPhones = await db.Users
                    .Where(u => u.Id != user.Id && u.PhoneNumber != null)
                    .Select(u => u.PhoneNumber!)
                    .ToListAsync(cancellationToken);

                if (otherPhones.Any(existing => PhoneNormalizer.Normalize(existing) == normalized))
                {
                    return BadRequest(new { error = ApiErrors.ProfileUpdateFailed });
                }
            }

            user.PhoneNumber = phone;
        }

        if (request.PreferredName is not null)
        {
            var preferredName = string.IsNullOrWhiteSpace(request.PreferredName)
                ? null
                : request.PreferredName.Trim();

            if (preferredName is not null)
            {
                var preferredNameError = RegistrationValidator.ValidatePreferredName(preferredName);
                if (preferredNameError is not null)
                {
                    return BadRequest(new { error = preferredNameError });
                }
            }

            user.PreferredName = preferredName;
        }

        if (request.Gender is not null)
        {
            var gender = string.IsNullOrWhiteSpace(request.Gender)
                ? null
                : request.Gender.Trim();

            if (gender is not null)
            {
                var genderError = RegistrationValidator.ValidateGender(gender, AllowedGenders);
                if (genderError is not null)
                {
                    return BadRequest(new { error = genderError });
                }

                gender = AllowedGenders.First(g =>
                    string.Equals(g, gender, StringComparison.OrdinalIgnoreCase));
            }

            user.Gender = gender;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        if (emailChanged)
        {
            await emailVerification.IssueVerificationEmailAsync(user, cancellationToken);
        }

        await EnsureProfileDefaultsAsync(user, cancellationToken);
        return Ok(ToProfile(user));
    }

    [HttpPost("me/change-password")]
    [ProducesResponseType(typeof(ChangePasswordResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChangePasswordResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.TryGetUserId();
        if (userId is null)
        {
            return Unauthorized(new { error = "Sign in to change your password." });
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { error = "Current and new password are required." });
        }

        var passwordError = RegistrationValidator.ValidatePassword(request.NewPassword);
        if (passwordError is not null)
        {
            return BadRequest(new { error = passwordError });
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { error = "User not found." });
        }

        if (!passwords.Verify(user.PasswordHash, request.CurrentPassword))
        {
            return BadRequest(new { error = "Current password is incorrect." });
        }

        user.PasswordHash = passwords.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        securityStamps.RotateStamp(user);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var token = jwtTokens.CreateToken(user);
        authCookies.SetAuthCookie(Response, token);
        SecurityAuditLogger.LogPasswordChanged(logger, user.Id);

        return Ok(new ChangePasswordResponse("Your password has been updated."));
    }

    private async Task EnsureProfileDefaultsAsync(Domain.Entities.User user, CancellationToken cancellationToken)
    {
        var changed = false;

        if (string.IsNullOrWhiteSpace(user.FriendCode) || IsNameBasedFriendCode(user))
        {
            user.FriendCode = await friendCodes.AllocateUniqueFriendCodeAsync(cancellationToken);
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(user.FirstName))
        {
            user.FirstName = user.DisplayName?.Split(' ').FirstOrDefault() ?? "Friend";
            changed = true;
        }

        if (changed)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public static UserProfileDto ToProfile(Domain.Entities.User user) =>
        new(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PreferredName,
            user.Gender,
            user.PhoneNumber,
            user.FriendCode,
            GetDisplayName(user));

    private static string GetDisplayName(Domain.Entities.User user)
    {
        if (!string.IsNullOrWhiteSpace(user.PreferredName))
        {
            return user.PreferredName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            return user.FirstName.Trim();
        }

        return user.DisplayName?.Trim() ?? "Friend";
    }

    private static bool IsNameBasedFriendCode(Domain.Entities.User user)
    {
        var code = user.FriendCode.Trim().ToUpperInvariant();
        if (code.Length == 0)
        {
            return false;
        }

        foreach (var name in new[] { user.FirstName, user.PreferredName, user.DisplayName })
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var prefix = name.Trim().Split(' ')[0].ToUpperInvariant();
            if (prefix.Length >= 3 && code.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
