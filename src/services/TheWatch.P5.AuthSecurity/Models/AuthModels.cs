using System.ComponentModel.DataAnnotations;

namespace TheWatch.P5.AuthSecurity.Auth;

// DTOs — using .Auth namespace to avoid collision with Roslyn-generated .Models types

public record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MinLength(15), MaxLength(128)] string Password,
    [property: Required, MaxLength(255)] string DisplayName,
    [property: MaxLength(20)] string? Phone = null);

public record LoginRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MinLength(1)] string Password,
    [property: MaxLength(6)] string? TotpCode = null,
    [property: MaxLength(500)] string? MfaToken = null,
    [property: MaxLength(500)] string? DeviceFingerprint = null);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User,
    bool MfaRequired = false,
    string? MfaToken = null,
    bool PasswordExpired = false);

public record RefreshTokenRequest(
    [property: Required, MinLength(10)] string RefreshToken,
    [property: MaxLength(500)] string? DeviceFingerprint = null);

public record UserInfo(
    Guid Id,
    string Email,
    string DisplayName,
    string? Phone,
    string[] Roles,
    DateTime CreatedAt,
    string? AcceptedEulaVersion = null);

public record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public record AssignRoleRequest(
    [property: Required] Guid UserId,
    [property: Required, MaxLength(50)] string Role);

public record MfaSetupResponse(
    string SharedKey,
    string AuthenticatorUri,
    string[] RecoveryCodes);

public record MfaVerifyRequest(
    [property: Required, MaxLength(6)] string Code);

public record SmsMfaSendRequest(
    [property: Required, Phone, MaxLength(20)] string PhoneNumber);

public record SmsMfaVerifyRequest(
    [property: Required, MaxLength(6)] string Code,
    [property: Required, Phone, MaxLength(20)] string PhoneNumber);

public record MagicLinkRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email);
