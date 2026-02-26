using Microsoft.EntityFrameworkCore;
using TheWatch.P5.AuthSecurity.Data;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Services;

/// <summary>
/// STRIDE threat model scanner. Hangfire job every 15 min scanning AuditEvents.
/// </summary>
public class StrideThreatService
{
    private readonly AuthIdentityDbContext _db;
    private readonly ILogger<StrideThreatService> _logger;

    public StrideThreatService(AuthIdentityDbContext db, ILogger<StrideThreatService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ScanAsync()
    {
        var since = DateTime.UtcNow.AddMinutes(-15);
        var recentEvents = await _db.AuditEvents.Where(a => a.Timestamp > since).ToListAsync();

        // Spoofing: same user from multiple IPs
        var multiIpUsers = recentEvents
            .Where(a => a.UserId.HasValue && a.IsSuccess)
            .GroupBy(a => a.UserId)
            .Where(g => g.Select(e => e.IpAddress).Distinct().Count() > 3)
            .ToList();

        foreach (var group in multiIpUsers)
        {
            await AddThreatAsync("Spoofing", "Multi-IP Login",
                $"User {group.Key} logged in from {group.Select(e => e.IpAddress).Distinct().Count()} different IPs in 15 min",
                "High", group.Key);
        }

        // Information Disclosure: user enumeration (many failed logins to different accounts from same IP)
        var enumAttempts = recentEvents
            .Where(a => !a.IsSuccess && a.EventType == "Login")
            .GroupBy(a => a.IpAddress)
            .Where(g => g.Select(e => e.UserId).Distinct().Count() > 5)
            .ToList();

        foreach (var group in enumAttempts)
        {
            await AddThreatAsync("InformationDisclosure", "User Enumeration",
                $"IP {group.Key} attempted login for {group.Select(e => e.UserId).Distinct().Count()} different accounts",
                "Medium", ipAddress: group.Key);
        }

        // Denial of Service: excessive rate limit hits
        var dosIps = recentEvents
            .Where(a => !a.IsSuccess)
            .GroupBy(a => a.IpAddress)
            .Where(g => g.Count() > 50)
            .ToList();

        foreach (var group in dosIps)
        {
            await AddThreatAsync("DenialOfService", "Rate Limit Abuse",
                $"IP {group.Key} generated {group.Count()} failed requests in 15 min",
                "High", ipAddress: group.Key);
        }

        // Elevation of Privilege: unauthorized role changes
        var roleChanges = recentEvents
            .Where(a => a.EventType == "AssignRole" && a.IsSuccess)
            .ToList();

        if (roleChanges.Count > 5)
        {
            await AddThreatAsync("ElevationOfPrivilege", "Bulk Role Assignment",
                $"{roleChanges.Count} role assignments in 15 min",
                "Critical");
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ThreatAssessment>> GetRecentThreatsAsync()
    {
        return await _db.ThreatAssessments
            .OrderByDescending(t => t.DetectedAt)
            .Take(100)
            .ToListAsync();
    }

    private async Task AddThreatAsync(string category, string ruleName, string evidence, string severity, Guid? userId = null, string? ipAddress = null)
    {
        _db.ThreatAssessments.Add(new ThreatAssessment
        {
            Category = category,
            RuleName = ruleName,
            Evidence = evidence,
            Severity = severity,
            UserId = userId,
            IpAddress = ipAddress
        });

        _logger.LogWarning("STRIDE Threat Detected: {Category}/{RuleName} — {Evidence}", category, ruleName, evidence);
    }
}
