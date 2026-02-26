namespace TheWatch.Shared.Contracts.Mobile;

// Shared auth DTOs for mobile client consumption.
// These mirror the P5 API surface without conflicting with server-side models.

public record LoginRequest(
    string Email,
    string Password);

public record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    string? Phone = null);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfoDto User);

public record RefreshTokenRequest(
    string RefreshToken);

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
