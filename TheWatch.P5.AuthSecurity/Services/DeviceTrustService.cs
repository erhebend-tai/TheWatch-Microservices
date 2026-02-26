using Microsoft.EntityFrameworkCore;
using TheWatch.P5.AuthSecurity.Data;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Services;

/// <summary>
/// Device trust scoring: known device (+40), same IP (+20), nearby location (+20), recent login (+10), device age (+10).
/// Score < 30 requires MFA step-up.
/// </summary>
public class DeviceTrustService
{
    private readonly AuthIdentityDbContext _db;

    public DeviceTrustService(AuthIdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CalculateTrustScoreAsync(Guid userId, string? fingerprint, string? ipAddress, string? location)
    {
        if (string.IsNullOrEmpty(fingerprint)) return 0;

        var device = await _db.DeviceTrusts
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Fingerprint == fingerprint);

        if (device is null)
        {
            // New device — low trust
            device = new DeviceTrust
            {
                UserId = userId,
                Fingerprint = fingerprint,
                IpAddress = ipAddress,
                Location = location,
                TrustScore = 10
            };
            _db.DeviceTrusts.Add(device);
            await _db.SaveChangesAsync();
            return 10;
        }

        // Known device — calculate score
        var score = 40; // Known device base

        if (device.IpAddress == ipAddress)
            score += 20;

        if (!string.IsNullOrEmpty(location) && device.Location == location)
            score += 20;

        if (device.LastSeenAt > DateTime.UtcNow.AddDays(-7))
            score += 10;

        if (device.FirstSeenAt < DateTime.UtcNow.AddDays(-30))
            score += 10;

        score = Math.Min(score, 100);

        // Update device record
        device.LoginCount++;
        device.LastSeenAt = DateTime.UtcNow;
        device.IpAddress = ipAddress;
        device.TrustScore = score;
        device.IsTrusted = score >= 30;
        await _db.SaveChangesAsync();

        return score;
    }

    public async Task<IReadOnlyList<DeviceTrust>> GetDevicesForUserAsync(Guid userId)
    {
        return await _db.DeviceTrusts
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastSeenAt)
            .ToListAsync();
    }
}
