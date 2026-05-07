using Microsoft.EntityFrameworkCore;
using TheWatch.P5.AuthSecurity.Data;
using TheWatch.Shared.Security;

namespace TheWatch.P5.AuthSecurity.Services;

/// <summary>
/// Item 294: Automated data purge service for P5 AuthSecurity.
/// Runs as Hangfire recurring jobs to delete expired data per <see cref="DataRetentionPolicy"/>.
/// [NIST MP-6, SI-12]
/// </summary>
public class DataPurgeService(AuthIdentityDbContext db, ILogger<DataPurgeService> logger)
{
    /// <summary>
    /// Deletes refresh tokens that have expired or been revoked beyond the session token retention period.
    /// Runs daily. Keeps revoked tokens for 24 hours after revocation for audit trail.
    /// </summary>
    public async Task PurgeExpiredRefreshTokensAsync()
    {
        var cutoff = DateTime.UtcNow - DataRetentionPolicy.SessionToken;
        var tokens = await db.RefreshTokens
            .Where(t => t.ExpiresAt < cutoff || (t.IsRevoked && t.RevokedAt < cutoff))
            .ToListAsync();

        if (tokens.Count > 0)
        {
            db.RefreshTokens.RemoveRange(tokens);
            await db.SaveChangesAsync();
            logger.LogInformation("DataPurge: Deleted {Count} expired/revoked refresh tokens (cutoff={Cutoff})",
                tokens.Count, cutoff);
        }
    }

    /// <summary>
    /// Purges audit log entries older than the 1-year retention period.
    /// Runs weekly at off-peak hours.
    /// </summary>
    public async Task PurgeOldAuditEventsAsync()
    {
        var cutoff = DateTime.UtcNow - DataRetentionPolicy.AuditLog;
        var events = await db.AuditEvents
            .Where(e => e.Timestamp < cutoff)
            .ToListAsync();

        if (events.Count > 0)
        {
            db.AuditEvents.RemoveRange(events);
            await db.SaveChangesAsync();
            logger.LogInformation("DataPurge: Deleted {Count} audit events older than {Days} days",
                events.Count, DataRetentionPolicy.AuditLog.TotalDays);
        }
    }
}
