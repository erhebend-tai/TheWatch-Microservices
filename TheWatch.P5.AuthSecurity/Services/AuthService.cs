using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TheWatch.P5.AuthSecurity.Auth;

namespace TheWatch.P5.AuthSecurity.Services;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<TokenPair> RefreshAsync(string refreshToken);
    Task<UserInfo?> GetUserByIdAsync(Guid userId);
    Task<IReadOnlyList<UserInfo>> ListUsersAsync();
}

public class AuthService : IAuthService
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ConcurrentDictionary<string, RefreshToken> _refreshTokens = new();
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    public Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        // Check for duplicate email
        if (_users.Values.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A user with this email already exists.");

        var user = new User
        {
            Email = request.Email,
            DisplayName = request.DisplayName,
            Phone = request.Phone,
            PasswordHash = HashPassword(request.Password)
        };

        if (!_users.TryAdd(user.Id, user))
            throw new InvalidOperationException("Failed to create user.");

        var tokens = GenerateTokens(user);
        var response = new LoginResponse(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt, user.ToUserInfo());
        return Task.FromResult(response);
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = _users.Values.FirstOrDefault(u =>
            u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        user.LastLoginAt = DateTime.UtcNow;

        var tokens = GenerateTokens(user);
        var response = new LoginResponse(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt, user.ToUserInfo());
        return Task.FromResult(response);
    }

    public Task<TokenPair> RefreshAsync(string refreshToken)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var stored))
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired or revoked.");

        if (!_users.TryGetValue(stored.UserId, out var user))
            throw new UnauthorizedAccessException("User not found.");

        // Revoke old token
        stored.IsRevoked = true;

        var tokens = GenerateTokens(user);
        return Task.FromResult(tokens);
    }

    public Task<UserInfo?> GetUserByIdAsync(Guid userId)
    {
        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user?.ToUserInfo());
    }

    public Task<IReadOnlyList<UserInfo>> ListUsersAsync()
    {
        var users = _users.Values.Select(u => u.ToUserInfo()).ToList();
        return Task.FromResult<IReadOnlyList<UserInfo>>(users);
    }

    private TokenPair GenerateTokens(User user)
    {
        var jwtKey = _config["Jwt:Key"] ?? "TheWatch-P5-AuthSecurity-DevKey-Min32Chars!!";
        var issuer = _config["Jwt:Issuer"] ?? "TheWatch.P5.AuthSecurity";
        var audience = _config["Jwt:Audience"] ?? "TheWatch";
        var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("display_name", user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var allClaims = claims.Concat(user.Roles.Select(r => new Claim(ClaimTypes.Role, r))).ToArray();

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var token = new JwtSecurityToken(issuer, audience, allClaims, expires: expiresAt, signingCredentials: creds);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Generate refresh token
        var refreshTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenObj = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _refreshTokens[refreshTokenValue] = refreshTokenObj;

        return new TokenPair(accessToken, refreshTokenValue, expiresAt);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        var combined = new byte[48];
        salt.CopyTo(combined, 0);
        hash.CopyTo(combined, 16);
        return Convert.ToBase64String(combined);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var combined = Convert.FromBase64String(storedHash);
        var salt = combined[..16];
        var storedHashBytes = combined[16..];
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, storedHashBytes);
    }
}
