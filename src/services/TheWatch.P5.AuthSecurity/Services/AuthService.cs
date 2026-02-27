using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TheWatch.P5.AuthSecurity.Data;
using TheWatch.P5.AuthSecurity.Models;
using TheWatch.Shared.Auth;

// Aliases for DTOs that clash with Roslyn-generated Models
using LoginRequest = TheWatch.P5.AuthSecurity.Auth.LoginRequest;
using LoginResponse = TheWatch.P5.AuthSecurity.Auth.LoginResponse;
using RegisterRequest = TheWatch.P5.AuthSecurity.Auth.RegisterRequest;
using RefreshTokenRequest = TheWatch.P5.AuthSecurity.Auth.RefreshTokenRequest;
using TokenPair = TheWatch.P5.AuthSecurity.Auth.TokenPair;
using UserInfo = TheWatch.P5.AuthSecurity.Auth.UserInfo;
using AssignRoleRequest = TheWatch.P5.AuthSecurity.Auth.AssignRoleRequest;
using MfaSetupResponse = TheWatch.P5.AuthSecurity.Auth.MfaSetupResponse;

namespace TheWatch.P5.AuthSecurity.Services;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<TokenPair> RefreshAsync(string refreshToken, string? deviceFingerprint = null, string? ipAddress = null);
    Task<UserInfo?> GetUserByIdAsync(Guid userId);
    Task<IReadOnlyList<UserInfo>> ListUsersAsync();
    Task AssignRoleAsync(Guid userId, string role);
}

public class AuthService : IAuthService
{
    private readonly UserManager<WatchUser> _userManager;
    private readonly AuthIdentityDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(UserManager<WatchUser> userManager, AuthIdentityDbContext db, IConfiguration config)
    {
        _userManager = userManager;
        _db = db;
        _config = config;
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new InvalidOperationException("A user with this email already exists.");

        var user = new WatchUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            PhoneNumber = request.Phone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        // Assign default Patient role
        await _userManager.AddToRoleAsync(user, WatchRoles.Patient);

        var roles = await _userManager.GetRolesAsync(user);
        var tokens = await GenerateTokensAsync(user, roles.ToArray());
        var userInfo = ToUserInfo(user, roles.ToArray());
        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt, userInfo);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        // Check lockout
        if (await _userManager.IsLockedOutAsync(user))
            throw new UnauthorizedAccessException("Account is locked. Try again later.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Reset access failed count on success
        await _userManager.ResetAccessFailedCountAsync(user);

        // Check if MFA is required
        if (user.IsTotpEnabled && string.IsNullOrEmpty(request.TotpCode))
        {
            var mfaToken = GenerateMfaToken(user.Id);
            return new LoginResponse("", "", DateTime.UtcNow, ToUserInfo(user, []), MfaRequired: true, MfaToken: mfaToken);
        }

        // If TOTP code provided, verify it
        if (user.IsTotpEnabled && !string.IsNullOrEmpty(request.TotpCode))
        {
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, request.TotpCode);
            if (!isValid)
                throw new UnauthorizedAccessException("Invalid TOTP code.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var tokens = await GenerateTokensAsync(user, roles.ToArray(), request.DeviceFingerprint);
        var userInfo = ToUserInfo(user, roles.ToArray());
        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt, userInfo);
    }

    public async Task<TokenPair> RefreshAsync(string refreshToken, string? deviceFingerprint = null, string? ipAddress = null)
    {
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
        if (stored is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (stored.IsRevoked)
        {
            await RevokeTokenChainAsync(stored.Token);
            throw new UnauthorizedAccessException("Refresh token has been revoked. Possible token compromise detected.");
        }

        if (stored.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired.");

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive.");

        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;

        var roles = await _userManager.GetRolesAsync(user);
        var newTokens = await GenerateTokensAsync(user, roles.ToArray(), deviceFingerprint, ipAddress);

        stored.ReplacedByToken = newTokens.RefreshToken;
        await _db.SaveChangesAsync();

        return newTokens;
    }

    public async Task<UserInfo?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        return ToUserInfo(user, roles.ToArray());
    }

    public async Task<IReadOnlyList<UserInfo>> ListUsersAsync()
    {
        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        var result = new List<UserInfo>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(ToUserInfo(user, roles.ToArray()));
        }
        return result;
    }

    public async Task AssignRoleAsync(Guid userId, string role)
    {
        if (!WatchRoles.All.Contains(role))
            throw new InvalidOperationException($"Unknown role: {role}");

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (!await _userManager.IsInRoleAsync(user, role))
            await _userManager.AddToRoleAsync(user, role);
    }

    private async Task<TokenPair> GenerateTokensAsync(WatchUser user, string[] roles, string? deviceFingerprint = null, string? ipAddress = null)
    {
        var jwtKey = _config["Jwt:Key"] ?? "TheWatch-P5-AuthSecurity-DevKey-Min32Chars!!";
        var issuer = _config["Jwt:Issuer"] ?? "TheWatch.P5.AuthSecurity";
        var audience = _config["Jwt:Audience"] ?? "TheWatch";
        var expiryMinutes = int.TryParse(_config["Jwt:AccessTokenLifetimeMinutes"], out var m) ? m : 15;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("display_name", user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (!string.IsNullOrEmpty(user.AcceptedEulaVersion))
            claims.Add(new Claim("eula_version", user.AcceptedEulaVersion));

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiresAt, signingCredentials: creds);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenObj = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            DeviceFingerprint = deviceFingerprint,
            IpAddress = ipAddress
        };
        _db.RefreshTokens.Add(refreshTokenObj);
        await _db.SaveChangesAsync();

        return new TokenPair(accessToken, refreshTokenValue, expiresAt);
    }

    private async Task RevokeTokenChainAsync(string token)
    {
        var current = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        while (current is not null)
        {
            if (!current.IsRevoked)
            {
                current.IsRevoked = true;
                current.RevokedAt = DateTime.UtcNow;
            }
            if (string.IsNullOrEmpty(current.ReplacedByToken)) break;
            current = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == current.ReplacedByToken);
        }
        await _db.SaveChangesAsync();
    }

    private static string GenerateMfaToken(Guid userId)
    {
        var payload = $"{userId}:{DateTime.UtcNow.AddMinutes(5).Ticks}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    public static Guid? ValidateMfaToken(string mfaToken)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(mfaToken));
            var parts = payload.Split(':');
            if (parts.Length != 2) return null;
            if (!Guid.TryParse(parts[0], out var userId)) return null;
            if (!long.TryParse(parts[1], out var ticks)) return null;
            if (new DateTime(ticks) < DateTime.UtcNow) return null;
            return userId;
        }
        catch { return null; }
    }

    private static UserInfo ToUserInfo(WatchUser user, string[] roles)
        => new(user.Id, user.Email!, user.DisplayName, user.PhoneNumber, roles, user.CreatedAt, user.AcceptedEulaVersion);
}
