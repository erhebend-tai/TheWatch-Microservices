using TheWatch.Contracts.CoreGateway.Models;

namespace TheWatch.Contracts.CoreGateway;

public interface ICoreGatewayClient
{
    Task<UserProfileDto> GetProfileAsync(Guid id, CancellationToken ct = default);
    Task<ProfileListResponse> ListProfilesAsync(int page = 1, int pageSize = 20, UserRole? role = null, CancellationToken ct = default);
    Task<UserProfileDto> CreateProfileAsync(CreateProfileRequest request, CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken ct = default);
    Task DeleteProfileAsync(Guid id, CancellationToken ct = default);
    Task SetPreferenceAsync(Guid profileId, SetPreferenceRequest request, CancellationToken ct = default);
    Task<PlatformConfigDto> GetConfigAsync(string key, CancellationToken ct = default);
    Task SetConfigAsync(SetConfigRequest request, CancellationToken ct = default);
    Task<ServiceHealthSummary> GetServiceHealthAsync(CancellationToken ct = default);
}
