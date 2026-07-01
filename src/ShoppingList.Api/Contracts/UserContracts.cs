namespace ShoppingList.Api.Contracts;

public record UserProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string? LastName,
    string? PreferredName,
    string? Gender,
    string? PhoneNumber,
    string FriendCode,
    string DisplayName);

public record UpdateUserProfileRequest(
    string? Email,
    string? PhoneNumber,
    string? PreferredName,
    string? Gender);

public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string Email,
    string Password,
    string PreferredName,
    string? Gender,
    string? PhoneNumber);

public record RegisterResponse(string Message);

public record VerifyEmailRequest(string Token);

public record VerifyEmailResponse(bool Verified, string Message);

public record ForgotPasswordRequest(string Email);

public record ForgotPasswordResponse(string Message);

public record ResetPasswordRequest(string Token, string Password);

public record ResetPasswordResponse(bool Success, string Message);

public record LoginResponse(UserProfileDto User);
