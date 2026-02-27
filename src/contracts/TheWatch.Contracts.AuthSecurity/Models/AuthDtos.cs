namespace TheWatch.Contracts.AuthSecurity.Models;

public record RegisterRequest(string Email, string Password, string DisplayName, string? Phone = null);
public record LoginRequest(string Email, string Password, string? TotpCode = null, string? MfaToken = null, string? DeviceFingerprint = null);
public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserInfoDto User, bool MfaRequired = false, string? MfaToken = null);
public record RefreshTokenRequest(string RefreshToken, string? DeviceFingerprint = null);
public record UserInfoDto(Guid Id, string Email, string DisplayName, string? Phone, string[] Roles, DateTime CreatedAt, string? AcceptedEulaVersion = null);
public record TokenPair(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record AssignRoleRequest(Guid UserId, string Role);
public record MfaSetupResponse(string SharedKey, string AuthenticatorUri, string[] RecoveryCodes);
public record MfaVerifyRequest(string Code);
public record SmsMfaSendRequest(string PhoneNumber);
public record SmsMfaVerifyRequest(string Code, string PhoneNumber);
public record MagicLinkRequest(string Email);
