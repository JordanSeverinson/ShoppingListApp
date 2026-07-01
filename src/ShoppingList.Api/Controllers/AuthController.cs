using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Api.Services;
using ShoppingList.Application.Users;
using ShoppingList.Domain.Entities;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(
    ApplicationDbContext db,
    PasswordService passwords,
    JwtTokenService jwtTokens,
    FriendCodeAllocationService friendCodes,
    EmailVerificationService emailVerification) : ControllerBase
{
    private static readonly HashSet<string> AllowedGenders =
        new(StringComparer.OrdinalIgnoreCase) { "Male", "Female", "Non-binary" };

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var preferredNameError = RegistrationValidator.ValidatePreferredName(request.PreferredName);
        if (preferredNameError is not null)
        {
            return BadRequest(new { error = preferredNameError });
        }

        var emailError = RegistrationValidator.ValidateEmail(request.Email);
        if (emailError is not null)
        {
            return BadRequest(new { error = emailError });
        }

        var phoneError = RegistrationValidator.ValidatePhone(request.PhoneNumber);
        if (phoneError is not null)
        {
            return BadRequest(new { error = phoneError });
        }

        var passwordError = RegistrationValidator.ValidatePassword(request.Password);
        if (passwordError is not null)
        {
            return BadRequest(new { error = passwordError });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await db.Users.AnyAsync(
            u => u.Email.ToLower() == email,
            cancellationToken);

        if (emailTaken)
        {
            return BadRequest(new { error = "That email is already in use." });
        }

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            phone = RegistrationValidator.FormatPhone(request.PhoneNumber);
            var normalized = PhoneNormalizer.Normalize(phone);
            var phoneTaken = await db.Users
                .Where(u => u.PhoneNumber != null)
                .Select(u => u.PhoneNumber!)
                .ToListAsync(cancellationToken);

            if (phoneTaken.Any(existing => PhoneNormalizer.Normalize(existing) == normalized))
            {
                return BadRequest(new { error = "That phone number is already in use." });
            }
        }

        string? gender = null;
        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            var trimmedGender = request.Gender.Trim();
            if (!AllowedGenders.Contains(trimmedGender))
            {
                return BadRequest(new { error = "Select a valid gender option." });
            }

            gender = AllowedGenders.First(g =>
                string.Equals(g, trimmedGender, StringComparison.OrdinalIgnoreCase));
        }

        var preferredName = request.PreferredName.Trim();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwords.Hash(request.Password),
            PreferredName = preferredName,
            FirstName = preferredName,
            DisplayName = preferredName,
            Gender = gender,
            PhoneNumber = phone,
            FriendCode = await friendCodes.AllocateUniqueFriendCodeAsync(cancellationToken),
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        await emailVerification.IssueVerificationEmailAsync(user, cancellationToken);

        return Ok(new RegisterResponse(
            "Account created. Check your email for a verification link before signing in."));
    }

    [HttpGet("verify-email")]
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VerifyEmailResponse>> VerifyEmail(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var verified = await emailVerification.VerifyAsync(token, cancellationToken);
        if (!verified)
        {
            return BadRequest(new VerifyEmailResponse(
                false,
                "This verification link is invalid or has expired."));
        }

        return Ok(new VerifyEmailResponse(
            true,
            "Your email has been verified. You can now sign in."));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Email and password are required." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == email,
            cancellationToken);

        if (user is null || !passwords.Verify(user.PasswordHash, request.Password))
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        if (!user.EmailVerified)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = "Verify your email before signing in.", code = "email_not_verified" });
        }

        var profile = UsersController.ToProfile(user);
        var token = jwtTokens.CreateToken(user);
        return Ok(new LoginResponse(token, profile));
    }
}
