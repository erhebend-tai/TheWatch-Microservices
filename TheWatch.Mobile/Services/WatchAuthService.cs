using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using TheWatch.Shared.Contracts.Mobile;

namespace TheWatch.Mobile.Services;

public class WatchAuthService
{
    private const string AccessTokenKey = "watch_access_token";
    private const string RefreshTokenKey = "watch_refresh_token";
    private const string ExpiresAtKey = "watch_expires_at";

    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _expiresAt;
    private UserInfoDto? _currentUser;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && _expiresAt > DateTime.UtcNow;
    public UserInfoDto? CurrentUser => _currentUser;

    public Task<Guid?> GetCurrentUserIdAsync()
    {
        return Task.FromResult(_currentUser?.Id);
    }

    public async Task InitializeAsync()
    {
        try
        {
            _accessToken = await SecureStorage.GetAsync(AccessTokenKey);
            _refreshToken = await SecureStorage.GetAsync(RefreshTokenKey);
            var expiresStr = await SecureStorage.GetAsync(ExpiresAtKey);
            if (DateTime.TryParse(expiresStr, out var exp))
                _expiresAt = exp;

            if (!string.IsNullOrEmpty(_accessToken))
                _currentUser = ParseUserFromToken(_accessToken);
        }
        catch
        {
            // SecureStorage may fail on certain platforms during init
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        if (string.IsNullOrEmpty(_accessToken))
            return null;

        // Auto-refresh if expiring within 5 minutes
        if (_expiresAt <= DateTime.UtcNow.AddMinutes(5) && !string.IsNullOrEmpty(_refreshToken))
        {
            await TryRefreshTokenAsync();
        }

        return _accessToken;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string baseUrl, string email, string password)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await http.PostAsJsonAsync(
                $"{baseUrl}/api/auth/login",
                new LoginRequest(email, password));

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return (false, body.Contains("not found") || body.Contains("Invalid")
                    ? "Invalid email or password."
                    : $"Login failed: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result is null) return (false, "Empty response from server.");

            await StoreTokensAsync(result.AccessToken, result.RefreshToken, result.ExpiresAt);
            _currentUser = result.User;
            OnAuthStateChanged?.Invoke();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Cannot reach auth service: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(
        string baseUrl, string email, string password, string displayName, string? phone)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await http.PostAsJsonAsync(
                $"{baseUrl}/api/auth/register",
                new RegisterRequest(email, password, displayName, phone));

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return (false, body.Contains("already exists")
                    ? "Email already registered."
                    : $"Registration failed: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result is null) return (false, "Empty response from server.");

            await StoreTokensAsync(result.AccessToken, result.RefreshToken, result.ExpiresAt);
            _currentUser = result.User;
            OnAuthStateChanged?.Invoke();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Cannot reach auth service: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> ForgotPasswordAsync(string baseUrl, string email)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await http.PostAsJsonAsync(
                $"{baseUrl}/api/auth/forgot-password",
                new ForgotPasswordRequest(email));

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return (false, body.Contains("not found")
                    ? "Email address not found."
                    : $"Request failed: {response.StatusCode}");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Cannot reach auth service: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(
        string baseUrl, string email, string token, string newPassword)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await http.PostAsJsonAsync(
                $"{baseUrl}/api/auth/reset-password",
                new ResetPasswordRequest(email, token, newPassword));

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return (false, body.Contains("invalid") || body.Contains("expired")
                    ? "Invalid or expired reset token."
                    : $"Reset failed: {response.StatusCode}");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Cannot reach auth service: {ex.Message}");
        }
    }

    public async Task<EulaDto?> GetCurrentEulaAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await http.GetAsync($"{baseUrl}/api/eula/current");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<EulaDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> AcceptEulaAsync(string baseUrl)
    {
        try
        {
            var token = await GetAccessTokenAsync();
            if (token is null) return false;
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await http.PostAsync($"{baseUrl}/api/eula/accept", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        _expiresAt = DateTime.MinValue;
        _currentUser = null;

        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
        SecureStorage.Remove(ExpiresAtKey);

        await Task.CompletedTask;
        OnAuthStateChanged?.Invoke();
    }

    private async Task StoreTokensAsync(string accessToken, string refreshToken, DateTime expiresAt)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _expiresAt = expiresAt;

        await SecureStorage.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);
        await SecureStorage.SetAsync(ExpiresAtKey, expiresAt.ToString("O"));
    }

    private async Task TryRefreshTokenAsync()
    {
        if (!await _refreshLock.WaitAsync(0)) return;
        try
        {
            // Re-check after acquiring lock
            if (_expiresAt > DateTime.UtcNow.AddMinutes(5)) return;

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            // Determine base URL from stored token issuer or use default
            var p5Url = "http://localhost:5005";
            var response = await http.PostAsJsonAsync(
                $"{p5Url}/api/auth/refresh",
                new RefreshTokenRequest(_refreshToken!));

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TokenPair>();
                if (result is not null)
                {
                    await StoreTokensAsync(result.AccessToken, result.RefreshToken, result.ExpiresAt);
                    _currentUser = ParseUserFromToken(result.AccessToken);
                }
            }
            else
            {
                // Refresh failed — force logout
                await LogoutAsync();
            }
        }
        catch
        {
            // Silently fail — will re-prompt login on next protected action
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static UserInfoDto? ParseUserFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "";
            var name = jwt.Claims.FirstOrDefault(c => c.Type == "display_name")?.Value ?? email;
            var roles = jwt.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToArray();

            return new UserInfoDto(
                Guid.TryParse(sub, out var id) ? id : Guid.Empty,
                email,
                name,
                null,
                roles.Length > 0 ? roles : ["user"],
                DateTime.UtcNow);
        }
        catch
        {
            return null;
        }
    }
}
