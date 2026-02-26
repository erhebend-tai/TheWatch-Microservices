using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.CoreGateway.Models;

namespace TheWatch.Contracts.CoreGateway;

public class CoreGatewayClient(HttpClient http) : ServiceClientBase(http, "CoreGateway"), ICoreGatewayClient
{
    public Task<UserProfileDto> GetProfileAsync(Guid id, CancellationToken ct)
        => GetAsync<UserProfileDto>($"/api/profiles/{id}", ct);

    public Task<ProfileListResponse> ListProfilesAsync(int page, int pageSize, UserRole? role, CancellationToken ct)
    {
        var query = $"/api/profiles?page={page}&pageSize={pageSize}";
        if (role.HasValue) query += $"&role={role.Value}";
        return GetAsync<ProfileListResponse>(query, ct);
    }

    public Task<UserProfileDto> CreateProfileAsync(CreateProfileRequest request, CancellationToken ct)
        => PostAsync<UserProfileDto>("/api/profiles", request, ct);

    public Task<UserProfileDto> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken ct)
        => PutAsync<UserProfileDto>($"/api/profiles/{id}", request, ct);

    public Task DeleteProfileAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"/api/profiles/{id}", ct);

    public Task SetPreferenceAsync(Guid profileId, SetPreferenceRequest request, CancellationToken ct)
        => PostAsync($"/api/profiles/{profileId}/preferences", request, ct);

    public Task<PlatformConfigDto> GetConfigAsync(string key, CancellationToken ct)
        => GetAsync<PlatformConfigDto>($"/api/config/{key}", ct);

    public Task SetConfigAsync(SetConfigRequest request, CancellationToken ct)
        => PostAsync("/api/config", request, ct);

    public Task<ServiceHealthSummary> GetServiceHealthAsync(CancellationToken ct)
        => GetAsync<ServiceHealthSummary>("/api/services/health", ct);
}
