using Microsoft.EntityFrameworkCore;
using TheWatch.P4.Wearable.Devices;

namespace TheWatch.P4.Wearable.Data.Seeders;

public class WearableSeeder : IWatchDataSeeder
{
    public async Task SeedAsync(WearableDbContext context, CancellationToken ct = default)
    {
        if (await context.Set<WearableDevice>().AnyAsync(ct))
            return;

        var userId1 = Guid.Parse("00000000-0000-0000-0000-000000010001");
        var userId2 = Guid.Parse("00000000-0000-0000-0000-000000010002");

        // Devices
        var devices = new[]
        {
            new WearableDevice { Id = Guid.Parse("00000000-0000-0000-0004-000000000001"), Name = "Alice's Apple Watch", Platform = DevicePlatform.AppleWatch, Model = "Series 9", FirmwareVersion = "10.2", OwnerId = userId1, Status = DeviceStatus.Connected, BatteryPercent = 85, LastSyncAt = DateTime.UtcNow.AddMinutes(-5), HasHeartRateSensor = true, HasGpsSensor = true, HasAccelerometer = true, HasBloodOxygen = true, HasEcg = true, ScreenShape = "Round", ScreenWidthPx = 396, ScreenHeightPx = 484, BatteryCapacityMah = 308 },
            new WearableDevice { Id = Guid.Parse("00000000-0000-0000-0004-000000000002"), Name = "Bob's Garmin", Platform = DevicePlatform.Garmin, Model = "Fenix 7", FirmwareVersion = "14.20", OwnerId = userId2, Status = DeviceStatus.Connected, BatteryPercent = 72, LastSyncAt = DateTime.UtcNow.AddMinutes(-30), HasHeartRateSensor = true, HasGpsSensor = true, HasAccelerometer = true, HasBloodOxygen = true, ScreenShape = "Round", BatteryCapacityMah = 480 },
            new WearableDevice { Id = Guid.Parse("00000000-0000-0000-0004-000000000003"), Name = "Alice's Fitbit", Platform = DevicePlatform.Fitbit, Model = "Charge 6", FirmwareVersion = "2.1.8", OwnerId = userId1, Status = DeviceStatus.Disconnected, BatteryPercent = 15, LastSyncAt = DateTime.UtcNow.AddHours(-6), HasHeartRateSensor = true, HasGpsSensor = false, HasAccelerometer = true, ScreenShape = "Rectangle" }
        };
        context.Set<WearableDevice>().AddRange(devices);

        // Heartbeat Readings (20 readings across devices)
        var readings = new List<HeartbeatReading>();
        var rng = new Random(42);
        for (int i = 0; i < 20; i++)
        {
            var device = devices[i % 3];
            readings.Add(new HeartbeatReading
            {
                Id = Guid.Parse($"00000000-0000-0000-0004-000000001{i:D3}"),
                DeviceId = device.Id,
                Bpm = 60 + rng.Next(40),
                StepCount = rng.Next(100, 2000),
                CaloriesBurned = rng.Next(50, 500),
                RecordedAt = DateTime.UtcNow.AddMinutes(-i * 5)
            });
        }
        context.Set<HeartbeatReading>().AddRange(readings);

        await context.SaveChangesAsync(ct);
    }
}
