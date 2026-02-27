using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TheWatch.Mobile.Services;

namespace TheWatch.Mobile.Auth;

public class MobileAuthStateProvider : AuthenticationStateProvider
{
    private readonly WatchAuthService _authService;
    private bool _initialized;

    public MobileAuthStateProvider(WatchAuthService authService)
    {
        _authService = authService;
        _authService.OnAuthStateChanged += () =>
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_initialized)
        {
            await _authService.InitializeAsync();
            _initialized = true;
        }

        var user = _authService.CurrentUser;
        if (user is null || !_authService.IsAuthenticated)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
        };

        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
