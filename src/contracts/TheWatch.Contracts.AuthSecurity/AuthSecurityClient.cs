using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.AuthSecurity.Models;

namespace TheWatch.Contracts.AuthSecurity;

public class AuthSecurityClient(HttpClient http) : ServiceClientBase(http, "AuthSecurity"), IAuthSecurityClient
{
    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
        => PostAsync<LoginResponse>("/api/auth/login", request, ct);

    public Task<UserInfoDto> RegisterAsync(RegisterRequest request, CancellationToken ct)
        => PostAsync<UserInfoDto>("/api/auth/register", request, ct);

    public Task<TokenPair> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct)
        => PostAsync<TokenPair>("/api/auth/refresh", request, ct);

    public Task<UserInfoDto> GetUserInfoAsync(Guid userId, CancellationToken ct)
        => GetAsync<UserInfoDto>($"/api/users/{userId}", ct);

    public Task AssignRoleAsync(AssignRoleRequest request, CancellationToken ct)
        => PostAsync("/api/auth/roles/assign", request, ct);

    public Task<MfaSetupResponse> SetupMfaAsync(Guid userId, CancellationToken ct)
        => PostAsync<MfaSetupResponse>($"/api/auth/mfa/setup/{userId}", ct);

    public Task VerifyMfaAsync(Guid userId, MfaVerifyRequest request, CancellationToken ct)
        => PostAsync($"/api/auth/mfa/verify/{userId}", request, ct);

    public Task SendSmsMfaAsync(SmsMfaSendRequest request, CancellationToken ct)
        => PostAsync("/api/auth/mfa/sms/send", request, ct);

    public Task VerifySmsMfaAsync(SmsMfaVerifyRequest request, CancellationToken ct)
        => PostAsync("/api/auth/mfa/sms/verify", request, ct);

    public Task SendMagicLinkAsync(MagicLinkRequest request, CancellationToken ct)
        => PostAsync("/api/auth/magic-link", request, ct);
}
