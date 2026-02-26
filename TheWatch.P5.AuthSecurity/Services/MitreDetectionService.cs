using Microsoft.EntityFrameworkCore;
using TheWatch.P5.AuthSecurity.Data;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Services;

/// <summary>
/// MITRE ATT&CK detection rules engine. Hangfire job scanning AuditEvents against configured rules.
/// </summary>
public class MitreDetectionService
{
    private readonly AuthIdentityDbContext _db;
    private readonly ILogger<MitreDetectionService> _logger;

    public MitreDetectionService(AuthIdentityDbContext db, ILogger<MitreDetectionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ScanAsync()
    {
        var rules = await _db.AttackDetectionRules.Where(r => r.IsEnabled).ToListAsync();

        foreach (var rule in rules)
        {
            var since = DateTime.UtcNow.AddMinutes(-rule.TimeWindowMinutes);

            switch (rule.TechniqueId)
            {
                case "T1078": // Credential stuffing — many failed logins from same IP
                    await DetectCredentialStuffingAsync(rule, since);
                    break;
                case "T1110": // Brute force — many failed logins for same account
                    await DetectBruteForceAsync(rule, since);
                    break;
                case "T1528": // Token reuse from different IPs
                    await DetectTokenReuseAsync(rule, since);
                    break;
                case "T1621": // MFA fatigue — many MFA attempts
                    await DetectMfaFatigueAsync(rule, since);
                    break;
                case "T1556": // Modify authentication process
                    await DetectAuthModificationAsync(rule, since);
                    break;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AttackDetectionRule>> GetRulesAsync()
    {
        return await _db.AttackDetectionRules.ToListAsync();
    }

    private async Task DetectCredentialStuffingAsync(AttackDetectionRule rule, DateTime since)
    {
        var suspiciousIps = await _db.AuditEvents
            .Where(a => a.Timestamp > since && a.EventType == "Login" && !a.IsSuccess)
            .GroupBy(a => a.IpAddress)
            .Where(g => g.Count() >= rule.ThresholdCount)
            .Select(g => new { Ip = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var ip in suspiciousIps)
        {
            await AddDetectionAsync(rule, $"IP {ip.Ip} had {ip.Count} failed logins in {rule.TimeWindowMinutes} min", ipAddress: ip.Ip);
        }
    }

    private async Task DetectBruteForceAsync(AttackDetectionRule rule, DateTime since)
    {
        var suspiciousUsers = await _db.AuditEvents
            .Where(a => a.Timestamp > since && a.EventType == "Login" && !a.IsSuccess && a.UserId.HasValue)
            .GroupBy(a => a.UserId)
            .Where(g => g.Count() >= rule.ThresholdCount)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var user in suspiciousUsers)
        {
            await AddDetectionAsync(rule, $"User {user.UserId} had {user.Count} failed logins in {rule.TimeWindowMinutes} min", userId: user.UserId);
        }
    }

    private async Task DetectTokenReuseAsync(AttackDetectionRule rule, DateTime since)
    {
        var suspiciousRefreshes = await _db.AuditEvents
            .Where(a => a.Timestamp > since && a.EventType == "RefreshToken" && !a.IsSuccess)
            .GroupBy(a => a.UserId)
            .Where(g => g.Count() >= rule.ThresholdCount)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var user in suspiciousRefreshes)
        {
            await AddDetectionAsync(rule, $"User {user.UserId} had {user.Count} failed token refreshes (possible token theft)", userId: user.UserId);
        }
    }

    private async Task DetectMfaFatigueAsync(AttackDetectionRule rule, DateTime since)
    {
        var suspiciousMfa = await _db.AuditEvents
            .Where(a => a.Timestamp > since && a.EventType.Contains("Mfa") && !a.IsSuccess)
            .GroupBy(a => a.UserId)
            .Where(g => g.Count() >= rule.ThresholdCount)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var user in suspiciousMfa)
        {
            await AddDetectionAsync(rule, $"User {user.UserId} had {user.Count} failed MFA attempts (possible MFA fatigue attack)", userId: user.UserId);
        }
    }

    private async Task DetectAuthModificationAsync(AttackDetectionRule rule, DateTime since)
    {
        var roleChanges = await _db.AuditEvents
            .Where(a => a.Timestamp > since && a.EventType == "AssignRole")
            .CountAsync();

        if (roleChanges >= rule.ThresholdCount)
        {
            await AddDetectionAsync(rule, $"{roleChanges} role modifications detected in {rule.TimeWindowMinutes} min");
        }
    }

    private Task AddDetectionAsync(AttackDetectionRule rule, string evidence, Guid? userId = null, string? ipAddress = null)
    {
        _db.ThreatAssessments.Add(new ThreatAssessment
        {
            Category = $"MITRE:{rule.TechniqueId}",
            RuleName = rule.TechniqueName,
            Evidence = evidence,
            Severity = rule.Severity,
            UserId = userId,
            IpAddress = ipAddress
        });

        _logger.LogWarning("MITRE Detection: {TechniqueId} {TechniqueName} — {Evidence}", rule.TechniqueId, rule.TechniqueName, evidence);
        return Task.CompletedTask;
    }
}
