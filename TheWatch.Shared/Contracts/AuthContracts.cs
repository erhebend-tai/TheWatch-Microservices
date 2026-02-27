using System.ComponentModel.DataAnnotations;

namespace TheWatch.Shared.Contracts.Mobile;

// Shared auth DTOs for mobile client consumption.
// These mirror the P5 API surface without conflicting with server-side models.

public record LoginRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MinLength(1)] string Password);

public record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MinLength(8), MaxLength(128)] string Password,
    [property: Required, MaxLength(255)] string DisplayName,
    [property: MaxLength(20)] string? Phone = null);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfoDto User);

public record RefreshTokenRequest(
    [property: Required, MinLength(10)] string RefreshToken);

public record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public record UserInfoDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? Phone,
    string[] Roles,
    DateTime CreatedAt);

public record ForgotPasswordRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email);

public record ResetPasswordRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required] string Token,
    [property: Required, MinLength(8), MaxLength(128)] string NewPassword);

public record EulaDto(
    Guid Id,
    string Version,
    string Content,
    DateTime PublishedAt,
    bool IsCurrent);
