using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
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
    FriendCodeAllocationService friendCodes) : ControllerBase
{
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

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var emailTaken = await db.Users.AnyAsync(
                u => u.Id != user.Id && u.Email.ToLower() == email,
                cancellationToken);

            if (emailTaken)
            {
                return BadRequest(new { error = "That email is already in use." });
            }

            user.Email = email;
        }

        if (request.PhoneNumber is not null)
        {
            var phone = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim();

            if (phone is not null)
            {
                var normalized = PhoneNormalizer.Normalize(phone);
                var otherPhones = await db.Users
                    .Where(u => u.Id != user.Id && u.PhoneNumber != null)
                    .Select(u => u.PhoneNumber!)
                    .ToListAsync(cancellationToken);

                if (otherPhones.Any(existing => PhoneNormalizer.Normalize(existing) == normalized))
                {
                    return BadRequest(new { error = "That phone number is already in use." });
                }
            }

            user.PhoneNumber = phone;
        }

        if (request.PreferredName is not null)
        {
            user.PreferredName = string.IsNullOrWhiteSpace(request.PreferredName)
                ? null
                : request.PreferredName.Trim();
        }

        if (request.Gender is not null)
        {
            user.Gender = string.IsNullOrWhiteSpace(request.Gender)
                ? null
                : request.Gender.Trim();
        }

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await EnsureProfileDefaultsAsync(user, cancellationToken);
        return Ok(ToProfile(user));
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
