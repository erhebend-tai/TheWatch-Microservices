using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.Wearable.Models;

namespace TheWatch.Contracts.Wearable;

public class WearableClient(HttpClient http) : ServiceClientBase(http, "Wearable"), IWearableClient
{
    public Task<WearableDeviceDto> GetDeviceAsync(Guid id, CancellationToken ct)
        => GetAsync<WearableDeviceDto>($"/api/devices/{id}", ct);

    public Task<DeviceListResponse> ListDevicesAsync(Guid? ownerId, CancellationToken ct)
    {
        var query = ownerId.HasValue ? $"/api/devices?ownerId={ownerId}" : "/api/devices";
        return GetAsync<DeviceListResponse>(query, ct);
    }

    public Task<WearableDeviceDto> RegisterDeviceAsync(RegisterDeviceRequest request, CancellationToken ct)
        => PostAsync<WearableDeviceDto>("/api/devices", request, ct);

    public Task<WearableDeviceDto> UpdateDeviceStatusAsync(Guid id, UpdateDeviceStatusRequest request, CancellationToken ct)
        => PutAsync<WearableDeviceDto>($"/api/devices/{id}/status", request, ct);

    public Task DeleteDeviceAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"/api/devices/{id}", ct);

    public Task<HeartbeatReadingDto> RecordHeartbeatAsync(Guid deviceId, RecordHeartbeatRequest request, CancellationToken ct)
        => PostAsync<HeartbeatReadingDto>($"/api/devices/{deviceId}/heartbeat", request, ct);

    public Task<HeartbeatHistory> GetHeartbeatHistoryAsync(Guid deviceId, CancellationToken ct)
        => GetAsync<HeartbeatHistory>($"/api/devices/{deviceId}/heartbeat", ct);

    public Task<SyncJobDto> StartSyncAsync(Guid deviceId, StartSyncRequest request, CancellationToken ct)
        => PostAsync<SyncJobDto>($"/api/devices/{deviceId}/sync", request, ct);
}
