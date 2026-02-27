using Microsoft.EntityFrameworkCore;
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
    Task<int> MarkStaleDevicesOfflineAsync(TimeSpan timeout);
    Task<int> CleanupOldHeartbeatReadingsAsync(TimeSpan olderThan);
}

public class DeviceService : IDeviceService
{
    private readonly IWatchRepository<WearableDevice> _devices;
    private readonly IWatchRepository<HeartbeatReading> _heartbeats;
    private readonly IWatchRepository<SyncJob> _syncJobs;

    public DeviceService(
        IWatchRepository<WearableDevice> devices,
        IWatchRepository<HeartbeatReading> heartbeats,
        IWatchRepository<SyncJob> syncJobs)
    {
        _devices = devices;
        _heartbeats = heartbeats;
        _syncJobs = syncJobs;
    }

    public async Task<WearableDevice> RegisterAsync(RegisterDeviceRequest request)
    {
        var device = new WearableDevice
        {
            Name = request.Name,
            Platform = request.Platform,
            OwnerId = request.OwnerId,
            Model = request.Model,
            FirmwareVersion = request.FirmwareVersion
        };

        return await _devices.AddAsync(device);
    }

    public async Task<WearableDevice?> GetByIdAsync(Guid id)
    {
        return await _devices.GetByIdAsync(id);
    }

    public async Task<DeviceListResponse> ListAsync(Guid? ownerId, DevicePlatform? platform)
    {
        var query = _devices.Query();

        if (ownerId.HasValue)
            query = query.Where(d => d.OwnerId == ownerId.Value);
        if (platform.HasValue)
            query = query.Where(d => d.Platform == platform.Value);

        var items = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
        return new DeviceListResponse(items, items.Count);
    }

    public async Task<WearableDevice?> UpdateStatusAsync(Guid id, UpdateDeviceStatusRequest request)
    {
        var device = await _devices.GetByIdAsync(id);
        if (device is null) return null;

        device.Status = request.Status;
        if (request.BatteryPercent.HasValue) device.BatteryPercent = request.BatteryPercent;

        await _devices.UpdateAsync(device);
        return device;
    }

    public async Task<HeartbeatReading> RecordHeartbeatAsync(Guid deviceId, RecordHeartbeatRequest request)
    {
        var reading = new HeartbeatReading
        {
            DeviceId = deviceId,
            Bpm = request.Bpm,
            StepCount = request.StepCount,
            CaloriesBurned = request.CaloriesBurned
        };

        await _heartbeats.AddAsync(reading);

        // Update device last sync time
        var device = await _devices.GetByIdAsync(deviceId);
        if (device is not null)
        {
            device.LastSyncAt = DateTime.UtcNow;
            await _devices.UpdateAsync(device);
        }

        return reading;
    }

    public async Task<HeartbeatHistory> GetHeartbeatHistoryAsync(Guid deviceId, int limit)
    {
        var allCount = await _heartbeats.Query().Where(h => h.DeviceId == deviceId).CountAsync();
        var readings = await _heartbeats.Query()
            .Where(h => h.DeviceId == deviceId)
            .OrderByDescending(h => h.RecordedAt)
            .Take(limit)
            .ToListAsync();

        return new HeartbeatHistory(readings, allCount);
    }

    public async Task<SyncJob> StartSyncAsync(Guid deviceId, StartSyncRequest request)
    {
        var recordCount = await _heartbeats.Query().Where(h => h.DeviceId == deviceId).CountAsync();

        var job = new SyncJob
        {
            DeviceId = deviceId,
            Direction = request.Direction,
            Success = true,
            CompletedAt = DateTime.UtcNow,
            RecordsProcessed = recordCount
        };

        await _syncJobs.AddAsync(job);

        var device = await _devices.GetByIdAsync(deviceId);
        if (device is not null)
        {
            device.LastSyncAt = DateTime.UtcNow;
            device.Status = DeviceStatus.Connected;
            await _devices.UpdateAsync(device);
        }

        return job;
    }

    public async Task<List<SyncJob>> GetSyncHistoryAsync(Guid deviceId, int limit)
    {
        return await _syncJobs.Query()
            .Where(s => s.DeviceId == deviceId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> MarkStaleDevicesOfflineAsync(TimeSpan timeout)
    {
        var cutoff = DateTime.UtcNow - timeout;
        var stale = await _devices.Query()
            .Where(d => d.Status == DeviceStatus.Connected && d.LastSyncAt < cutoff)
            .ToListAsync();

        foreach (var device in stale)
        {
            device.Status = DeviceStatus.Disconnected;
            await _devices.UpdateAsync(device);
        }

        return stale.Count;
    }

    public async Task<int> CleanupOldHeartbeatReadingsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var old = await _heartbeats.Query()
            .Where(h => h.RecordedAt < cutoff)
            .Select(h => h.Id)
            .ToListAsync();

        foreach (var id in old)
            await _heartbeats.DeleteAsync(id);

        return old.Count;
    }
}
