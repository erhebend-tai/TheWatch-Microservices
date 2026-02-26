using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace TheWatch.Admin.Services;

public class AdminAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public string? AccessToken { get; private set; }

    public AdminAuthStateProvider(
        ProtectedSessionStorage sessionStorage,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _sessionStorage = sessionStorage;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<string>("access_token");
            if (result.Success && !string.IsNullOrEmpty(result.Value))
            {
                var principal = ParseTokenClaims(result.Value);

                // Enforce Admin role — non-admins are treated as anonymous
                if (!HasAdminRole(principal))
                {
                    await ClearTokensAsync();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                AccessToken = result.Value;
                _currentUser = principal;
            }
        }
        catch
        {
            // ProtectedSessionStorage throws during prerender
        }

        return new AuthenticationState(_currentUser);
    }

    public async Task LoginAsync(string accessToken, string refreshToken)
    {
        var principal = ParseTokenClaims(accessToken);

        // Enforce Admin role — reject non-admin users immediately
        if (!HasAdminRole(principal))
        {
            throw new InvalidOperationException("Access denied. Admin role required.");
        }

        AccessToken = accessToken;
        await _sessionStorage.SetAsync("access_token", accessToken);
        await _sessionStorage.SetAsync("refresh_token", refreshToken);
        _currentUser = principal;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public async Task LogoutAsync()
    {
        AccessToken = null;
        await ClearTokensAsync();
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public HttpClient CreateAuthorizedClient()
    {
        var client = _httpClientFactory.CreateClient("microservices");
        if (!string.IsNullOrEmpty(AccessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
        }
        return client;
    }

    private static ClaimsPrincipal ParseTokenClaims(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo < DateTime.UtcNow)
                return new ClaimsPrincipal(new ClaimsIdentity());

            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    private static bool HasAdminRole(ClaimsPrincipal principal)
    {
        if (principal.Identity is not { IsAuthenticated: true })
            return false;

        return principal.Claims.Any(c =>
            c.Type == ClaimTypes.Role && c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase));
    }

    private async Task ClearTokensAsync()
    {
        try
        {
            await _sessionStorage.DeleteAsync("access_token");
            await _sessionStorage.DeleteAsync("refresh_token");
        }
        catch
        {
            // Swallow — may fail during prerender or if session is already cleared
        }
    }
}
