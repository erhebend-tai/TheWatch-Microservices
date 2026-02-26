using System.Collections.Concurrent;
using TheWatch.P4.Wearable.Devices;

namespace TheWatch.P4.Wearable.Services;

public interface IDeviceService
{
    Task<WearableDevice> RegisterAsync(RegisterDeviceRequest request);
    Task<WearableDevice?> GetByIdAsync(Guid id);
    Task<DeviceListResponse> ListAsync(Guid? ownerId = null, DevicePlatform? platform = null);
    Task<WearableDevice?> UpdateStatusAsync(Guid id, UpdateDeviceStatusRequest request);
    Task<HeartbeatReading> RecordHeartbeatAsync(Guid deviceId, RecordHeartbeatRequest request);
    Task<HeartbeatHistory> GetHeartbeatHistoryAsync(Guid deviceId, int limit = 100);
    Task<SyncJob> StartSyncAsync(Guid deviceId, StartSyncRequest request);
    Task<List<SyncJob>> GetSyncHistoryAsync(Guid deviceId, int limit = 20);
}

public class DeviceService : IDeviceService
{
    private readonly ConcurrentDictionary<Guid, WearableDevice> _devices = new();
    private readonly ConcurrentDictionary<Guid, HeartbeatReading> _heartbeats = new();
    private readonly ConcurrentDictionary<Guid, SyncJob> _syncJobs = new();

    public Task<WearableDevice> RegisterAsync(RegisterDeviceRequest request)
    {
        var device = new WearableDevice
        {
            Name = request.Name,
            Platform = request.Platform,
            OwnerId = request.OwnerId,
            Model = request.Model,
            FirmwareVersion = request.FirmwareVersion
        };

        _devices[device.Id] = device;
        return Task.FromResult(device);
    }

    public Task<WearableDevice?> GetByIdAsync(Guid id)
    {
        _devices.TryGetValue(id, out var device);
        return Task.FromResult(device);
    }

    public Task<DeviceListResponse> ListAsync(Guid? ownerId, DevicePlatform? platform)
    {
        var query = _devices.Values.AsEnumerable();

        if (ownerId.HasValue)
            query = query.Where(d => d.OwnerId == ownerId.Value);
        if (platform.HasValue)
            query = query.Where(d => d.Platform == platform.Value);

        var items = query.OrderByDescending(d => d.CreatedAt).ToList();
        return Task.FromResult(new DeviceListResponse(items, items.Count));
    }

    public Task<WearableDevice?> UpdateStatusAsync(Guid id, UpdateDeviceStatusRequest request)
    {
        if (!_devices.TryGetValue(id, out var device))
            return Task.FromResult<WearableDevice?>(null);

        device.Status = request.Status;
        if (request.BatteryPercent.HasValue) device.BatteryPercent = request.BatteryPercent;

        return Task.FromResult<WearableDevice?>(device);
    }

    public Task<HeartbeatReading> RecordHeartbeatAsync(Guid deviceId, RecordHeartbeatRequest request)
    {
        var reading = new HeartbeatReading
        {
            DeviceId = deviceId,
            Bpm = request.Bpm,
            StepCount = request.StepCount,
            CaloriesBurned = request.CaloriesBurned
        };

        _heartbeats[reading.Id] = reading;

        // Update device last sync time
        if (_devices.TryGetValue(deviceId, out var device))
            device.LastSyncAt = DateTime.UtcNow;

        return Task.FromResult(reading);
    }

    public Task<HeartbeatHistory> GetHeartbeatHistoryAsync(Guid deviceId, int limit)
    {
        var all = _heartbeats.Values.Where(h => h.DeviceId == deviceId)
            .OrderByDescending(h => h.RecordedAt).ToList();
        var readings = all.Take(limit).ToList();
        return Task.FromResult(new HeartbeatHistory(readings, all.Count));
    }

    public Task<SyncJob> StartSyncAsync(Guid deviceId, StartSyncRequest request)
    {
        var job = new SyncJob
        {
            DeviceId = deviceId,
            Direction = request.Direction,
            Success = true,
            CompletedAt = DateTime.UtcNow,
            RecordsProcessed = _heartbeats.Values.Count(h => h.DeviceId == deviceId)
        };

        _syncJobs[job.Id] = job;

        if (_devices.TryGetValue(deviceId, out var device))
        {
            device.LastSyncAt = DateTime.UtcNow;
            device.Status = DeviceStatus.Connected;
        }

        return Task.FromResult(job);
    }

    public Task<List<SyncJob>> GetSyncHistoryAsync(Guid deviceId, int limit)
    {
        return Task.FromResult(_syncJobs.Values
            .Where(s => s.DeviceId == deviceId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToList());
    }
}
