namespace TheWatch.P5.AuthSecurity.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    string? Phone = null);

public record LoginRequest(
    string Email,
    string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User);

public record RefreshTokenRequest(
    string RefreshToken);

public record UserInfo(
    Guid Id,
    string Email,
    string DisplayName,
    string? Phone,
    string[] Roles,
    DateTime CreatedAt);

public record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string[] Roles { get; set; } = ["user"];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    public UserInfo ToUserInfo() => new(Id, Email, DisplayName, Phone, Roles, CreatedAt);
}

public class RefreshToken
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }
}
