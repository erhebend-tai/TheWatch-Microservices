using TheWatch.Contracts.AuthSecurity.Models;

namespace TheWatch.Contracts.AuthSecurity;

public interface IAuthSecurityClient
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserInfoDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<TokenPair> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<UserInfoDto> GetUserInfoAsync(Guid userId, CancellationToken ct = default);
    Task AssignRoleAsync(AssignRoleRequest request, CancellationToken ct = default);
    Task<MfaSetupResponse> SetupMfaAsync(Guid userId, CancellationToken ct = default);
    Task VerifyMfaAsync(Guid userId, MfaVerifyRequest request, CancellationToken ct = default);
    Task SendSmsMfaAsync(SmsMfaSendRequest request, CancellationToken ct = default);
    Task VerifySmsMfaAsync(SmsMfaVerifyRequest request, CancellationToken ct = default);
    Task SendMagicLinkAsync(MagicLinkRequest request, CancellationToken ct = default);
}
