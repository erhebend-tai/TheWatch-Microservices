using TheWatch.Contracts.Wearable.Models;

namespace TheWatch.Contracts.Wearable;

public interface IWearableClient
{
    Task<WearableDeviceDto> GetDeviceAsync(Guid id, CancellationToken ct = default);
    Task<DeviceListResponse> ListDevicesAsync(Guid? ownerId = null, CancellationToken ct = default);
    Task<WearableDeviceDto> RegisterDeviceAsync(RegisterDeviceRequest request, CancellationToken ct = default);
    Task<WearableDeviceDto> UpdateDeviceStatusAsync(Guid id, UpdateDeviceStatusRequest request, CancellationToken ct = default);
    Task DeleteDeviceAsync(Guid id, CancellationToken ct = default);
    Task<HeartbeatReadingDto> RecordHeartbeatAsync(Guid deviceId, RecordHeartbeatRequest request, CancellationToken ct = default);
    Task<HeartbeatHistory> GetHeartbeatHistoryAsync(Guid deviceId, CancellationToken ct = default);
    Task<SyncJobDto> StartSyncAsync(Guid deviceId, StartSyncRequest request, CancellationToken ct = default);
}
