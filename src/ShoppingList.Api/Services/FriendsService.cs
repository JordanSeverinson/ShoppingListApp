using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Domain.Entities;
using ShoppingList.Domain.Enums;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Services;

public class FriendsService(ApplicationDbContext db)
{
    public const string FriendRequestMessage =
        "If a matching account exists, your friend request has been sent.";

    public async Task<FriendsResponse> GetFriendsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var friendships = await db.Friendships
            .AsNoTracking()
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f =>
                (f.RequesterId == userId || f.AddresseeId == userId)
                && f.Status != FriendshipStatus.Declined)
            .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
            .ToListAsync(cancellationToken);

        var friends = friendships
            .Where(f => f.Status == FriendshipStatus.Accepted)
            .Select(f => ToFriendSummary(OtherUser(f, userId)))
            .OrderBy(f => f.FirstName)
            .ToList();

        var incoming = friendships
            .Where(f => f.Status == FriendshipStatus.Pending && f.AddresseeId == userId)
            .Select(f => ToFriendRequest(f, userId))
            .ToList();

        var outgoing = friendships
            .Where(f => f.Status == FriendshipStatus.Pending && f.RequesterId == userId)
            .Select(f => ToFriendRequest(f, userId))
            .ToList();

        return new FriendsResponse(friends, incoming, outgoing);
    }

    public async Task<SendFriendRequestResponse> SendRequestAsync(
        Guid userId,
        SendFriendRequest request,
        CancellationToken cancellationToken)
    {
        var target = await FindUserAsync(request, cancellationToken);
        if (target is null)
        {
            return GenericFriendRequestResponse();
        }

        if (target.Id == userId)
        {
            throw new InvalidOperationException("You cannot add yourself as a friend.");
        }

        var existing = await FindExistingFriendshipAsync(userId, target.Id, cancellationToken);
        if (existing is not null)
        {
            switch (existing.Status)
            {
                case FriendshipStatus.Accepted:
                case FriendshipStatus.Pending when existing.RequesterId == userId:
                    return GenericFriendRequestResponse();
                case FriendshipStatus.Pending when existing.AddresseeId == userId:
                    await AcceptExistingRequestAsync(existing, cancellationToken);
                    return GenericFriendRequestResponse();
                default:
                    return GenericFriendRequestResponse();
            }
        }

        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            RequesterId = userId,
            AddresseeId = target.Id,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        db.Friendships.Add(friendship);
        await db.SaveChangesAsync(cancellationToken);

        return GenericFriendRequestResponse();
    }

    public async Task<FriendSummaryDto> AcceptRequestAsync(
        Guid userId,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var friendship = await db.Friendships
            .Include(f => f.Requester)
            .FirstOrDefaultAsync(
                f => f.Id == requestId && f.AddresseeId == userId,
                cancellationToken);

        if (friendship is null)
        {
            throw new InvalidOperationException("Friend request not found.");
        }

        if (friendship.Status == FriendshipStatus.Accepted)
        {
            return ToFriendSummary(friendship.Requester);
        }

        if (friendship.Status != FriendshipStatus.Pending)
        {
            throw new InvalidOperationException("This friend request can no longer be accepted.");
        }

        friendship.Status = FriendshipStatus.Accepted;
        friendship.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ToFriendSummary(friendship.Requester);
    }

    public async Task DeclineRequestAsync(
        Guid userId,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var friendship = await db.Friendships
            .FirstOrDefaultAsync(
                f => f.Id == requestId && f.AddresseeId == userId && f.Status == FriendshipStatus.Pending,
                cancellationToken);

        if (friendship is null)
        {
            throw new InvalidOperationException("Friend request not found.");
        }

        friendship.Status = FriendshipStatus.Declined;
        friendship.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFriendAsync(Guid userId, Guid friendUserId, CancellationToken cancellationToken)
    {
        var friendship = await FindExistingFriendshipAsync(userId, friendUserId, cancellationToken);
        if (friendship is null || friendship.Status != FriendshipStatus.Accepted)
        {
            throw new InvalidOperationException("Friend not found.");
        }

        db.Friendships.Remove(friendship);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static SendFriendRequestResponse GenericFriendRequestResponse() =>
        new(Guid.Empty, FriendshipStatus.Pending, FriendRequestMessage, null);

    private async Task AcceptExistingRequestAsync(
        Friendship existing,
        CancellationToken cancellationToken)
    {
        existing.Status = FriendshipStatus.Accepted;
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Friendship?> FindExistingFriendshipAsync(
        Guid userId,
        Guid otherUserId,
        CancellationToken cancellationToken) =>
        await db.Friendships
            .FirstOrDefaultAsync(
                f =>
                    (f.RequesterId == userId && f.AddresseeId == otherUserId)
                    || (f.RequesterId == otherUserId && f.AddresseeId == userId),
                cancellationToken);

    private async Task<User?> FindUserAsync(SendFriendRequest request, CancellationToken cancellationToken)
    {
        var hasEmail = !string.IsNullOrWhiteSpace(request.Email);
        var hasPhone = !string.IsNullOrWhiteSpace(request.PhoneNumber);
        var hasFriendCode = !string.IsNullOrWhiteSpace(request.FriendCode);

        var provided = (hasEmail ? 1 : 0) + (hasPhone ? 1 : 0) + (hasFriendCode ? 1 : 0);
        if (provided != 1)
        {
            throw new InvalidOperationException("Provide exactly one of email, phone number, or friend code.");
        }

        if (hasEmail)
        {
            var email = request.Email!.Trim().ToLowerInvariant();
            return await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
        }

        if (hasPhone)
        {
            var normalized = NormalizePhone(request.PhoneNumber!);
            if (normalized.Length < 7)
            {
                throw new InvalidOperationException("Enter a valid phone number.");
            }

            var users = await db.Users
                .AsNoTracking()
                .Where(u => u.PhoneNumber != null)
                .ToListAsync(cancellationToken);

            return users.FirstOrDefault(u => NormalizePhone(u.PhoneNumber!) == normalized);
        }

        var friendCode = request.FriendCode!.Trim().ToUpperInvariant();
        return await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.FriendCode == friendCode, cancellationToken);
    }

    private static string NormalizePhone(string phone) =>
        Regex.Replace(phone, @"\D", string.Empty);

    private static User OtherUser(Friendship friendship, Guid userId) =>
        friendship.RequesterId == userId ? friendship.Addressee : friendship.Requester;

    public static FriendSummaryDto ToFriendSummary(User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.FriendCode);

    private static FriendRequestDto ToFriendRequest(Friendship friendship, Guid userId)
    {
        var other = OtherUser(friendship, userId);
        return new FriendRequestDto(
            friendship.Id,
            other.Id,
            other.FirstName,
            other.LastName,
            other.Email,
            other.FriendCode,
            friendship.Status,
            friendship.AddresseeId == userId);
    }
}
