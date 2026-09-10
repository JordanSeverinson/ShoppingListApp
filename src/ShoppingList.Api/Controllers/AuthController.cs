using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Api.Security;
using ShoppingList.Api.Services;
using ShoppingList.Application.Users;
using ShoppingList.Domain.Entities;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    ApplicationDbContext db,
    PasswordService passwords,
    JwtTokenService jwtTokens,
    AuthCookieService authCookies,
    CsrfTokenService csrfTokens,
    FriendCodeAllocationService friendCodes,
    EmailVerificationService emailVerification,
    PasswordResetService passwordReset,
    JwtDenylistService jwtDenylist,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CsrfTokenResponse), StatusCodes.Status200OK)]
    public ActionResult<CsrfTokenResponse> GetCsrfToken()
    {
        var token = csrfTokens.IssueToken(Response);
        return Ok(new CsrfTokenResponse(token));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
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
            return BadRequest(new { error = "Unable to create an account with these details." });
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
                return BadRequest(new { error = "Unable to create an account with these details." });
            }
        }

        string? gender = null;
        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            var trimmedGender = request.Gender.Trim();
            var genderError = RegistrationValidator.ValidateGender(trimmedGender);
            if (genderError is not null)
            {
                return BadRequest(new { error = genderError });
            }

            gender = RegistrationValidator.CanonicalGender(trimmedGender);
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
            SecurityStamp = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        await emailVerification.IssueVerificationEmailAsync(user, cancellationToken);

        return Ok(new RegisterResponse(
            "Account created. Check your email for a verification link before signing in."));
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<VerifyEmailResponse>> VerifyEmailPost(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken) =>
        VerifyEmailCore(request.Token, cancellationToken);

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
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

        var passwordValid = user is not null && passwords.Verify(user.PasswordHash, request.Password);
        if (!passwordValid)
        {
            SecurityAuditLogger.LogLoginFailure(logger, email);
            return Unauthorized(new { error = "Invalid email or password." });
        }

        // Password matched — safe to say verification is required without enabling email enumeration.
        if (!user!.EmailVerified)
        {
            SecurityAuditLogger.LogLoginFailure(logger, email);
            try
            {
                await emailVerification.IssueVerificationEmailAsync(user, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to re-send verification email for {Email}", email);
            }

            return Unauthorized(new
            {
                error = "Please verify your email before signing in. Check your inbox for a verification link.",
            });
        }

        if (user.SecurityStamp == Guid.Empty)
        {
            user.SecurityStamp = Guid.NewGuid();
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        var profile = UsersController.ToProfile(user);
        var token = jwtTokens.CreateToken(user);
        authCookies.SetAuthCookie(Response, token);
        csrfTokens.IssueToken(Response);
        SecurityAuditLogger.LogLoginSuccess(logger, user.Id);
        return Ok(new LoginResponse(profile));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (Guid.TryParse(userId, out var parsedUserId))
        {
            SecurityAuditLogger.LogLogout(logger, parsedUserId);
        }

        var token = ExtractAuthToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            await jwtDenylist.RevokeAsync(token, cancellationToken);
        }

        authCookies.ClearAuthCookie(Response);
        csrfTokens.ClearToken(Response);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var emailError = RegistrationValidator.ValidateEmail(request.Email);
        if (emailError is not null)
        {
            return BadRequest(new { error = emailError });
        }

        await passwordReset.RequestResetAsync(request.Email, cancellationToken);
        return Ok(new ForgotPasswordResponse(PasswordResetService.ForgotPasswordMessage));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var passwordError = RegistrationValidator.ValidatePassword(request.Password);
        if (passwordError is not null)
        {
            return BadRequest(new { error = passwordError });
        }

        var success = await passwordReset.ResetPasswordAsync(
            request.Token,
            request.Password,
            cancellationToken);

        if (!success)
        {
            return BadRequest(new ResetPasswordResponse(
                false,
                "This reset link is invalid or has expired."));
        }

        return Ok(new ResetPasswordResponse(
            true,
            "Your password has been reset. You can now sign in."));
    }

    private async Task<ActionResult<VerifyEmailResponse>> VerifyEmailCore(
        string? token,
        CancellationToken cancellationToken)
    {
        var verified = await emailVerification.VerifyAsync(token ?? string.Empty, cancellationToken);
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

    private string? ExtractAuthToken()
    {
        if (Request.Cookies.TryGetValue(AuthConstants.CookieName, out var cookieToken)
            && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        var authHeader = Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        return null;
    }
}
