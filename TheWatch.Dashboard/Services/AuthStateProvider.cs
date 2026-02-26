using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace TheWatch.Dashboard.Services;

public class WatchAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public string? AccessToken { get; private set; }

    public WatchAuthStateProvider(
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
                AccessToken = result.Value;
                _currentUser = ParseTokenClaims(result.Value);
            }
        }
        catch
        {
            // ProtectedSessionStorage throws on prerender
        }

        return new AuthenticationState(_currentUser);
    }

    public async Task LoginAsync(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        await _sessionStorage.SetAsync("access_token", accessToken);
        await _sessionStorage.SetAsync("refresh_token", refreshToken);
        _currentUser = ParseTokenClaims(accessToken);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public async Task LogoutAsync()
    {
        AccessToken = null;
        await _sessionStorage.DeleteAsync("access_token");
        await _sessionStorage.DeleteAsync("refresh_token");
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
}
