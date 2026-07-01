using ShoppingList.Domain.Enums;

namespace ShoppingList.Api.Contracts;

public record FriendSummaryDto(
    Guid UserId,
    string FirstName,
    string? LastName,
    string Email,
    string FriendCode);

public record FriendRequestDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string? LastName,
    string Email,
    string FriendCode,
    FriendshipStatus Status,
    bool IsIncoming);

public record FriendsResponse(
    IReadOnlyList<FriendSummaryDto> Friends,
    IReadOnlyList<FriendRequestDto> IncomingRequests,
    IReadOnlyList<FriendRequestDto> OutgoingRequests);

public record SendFriendRequest(
    string? Email,
    string? PhoneNumber,
    string? FriendCode);

public record SendFriendRequestResponse(
    Guid RequestId,
    FriendshipStatus Status,
    string Message,
    FriendSummaryDto? Friend);
