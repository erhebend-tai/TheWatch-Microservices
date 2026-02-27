using Microsoft.EntityFrameworkCore;
using TheWatch.P5.AuthSecurity.Data;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Services;

/// <summary>
/// Audit logging service. Writes to DB (and optionally Kafka auth.audit topic).
/// </summary>
public class AuditService
{
    private readonly AuthIdentityDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(AuthIdentityDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(string eventType, Guid? userId, HttpContext ctx, bool isSuccess, string? failureReason = null)
    {
        var audit = new AuditEvent
        {
            EventType = eventType,
            UserId = userId,
            IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
            SourcePort = ctx.Connection.RemotePort,
            UserAgent = ctx.Request.Headers.UserAgent.ToString(),
            DeviceFingerprint = ctx.Request.Headers["X-Device-Fingerprint"].FirstOrDefault(),
            IsSuccess = isSuccess,
            FailureReason = failureReason
        };

        _db.AuditEvents.Add(audit);
        await _db.SaveChangesAsync();

        // STIG V-222441-449: log timestamp (UTC), user identity, source IP, source port,
        // event type, success/failure, device fingerprint, and user agent string.
        _logger.LogInformation(
            "Audit [{Timestamp:u}] EventType={EventType} UserId={UserId} IP={IpAddress} Port={SourcePort} " +
            "Success={IsSuccess} DeviceFingerprint={DeviceFingerprint} UserAgent={UserAgent} FailureReason={FailureReason}",
            audit.Timestamp, eventType, userId, audit.IpAddress, audit.SourcePort,
            isSuccess, audit.DeviceFingerprint, audit.UserAgent, failureReason);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetRecentEventsAsync(int page = 1, int pageSize = 50)
    {
        return await _db.AuditEvents
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
