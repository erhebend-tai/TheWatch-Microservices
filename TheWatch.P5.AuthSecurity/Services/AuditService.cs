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

    public async Task LogAsync(string eventType, Guid? userId, HttpContext ctx, bool isSuccess, string? failureReason = null, string? attemptedIdentity = null)
    {
        var audit = new AuditEvent
        {
            EventType = eventType,
            UserId = userId,
            AttemptedIdentity = attemptedIdentity,
            IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
            SourcePort = ctx.Connection.RemotePort,
            UserAgent = ctx.Request.Headers.UserAgent.ToString(),
            DeviceFingerprint = ctx.Request.Headers["X-Device-Fingerprint"].FirstOrDefault(),
            IsSuccess = isSuccess,
            FailureReason = failureReason
        };

        _db.AuditEvents.Add(audit);
        await _db.SaveChangesAsync();

        // STIG V-222441: log timestamp (implicit), identity, IP, port, event type, success/failure
        _logger.LogInformation(
            "Audit: {EventType} UserId={UserId} Identity={Identity} Success={IsSuccess} IP={IpAddress} Port={SourcePort}",
            eventType, userId, attemptedIdentity ?? userId?.ToString(), isSuccess, audit.IpAddress, audit.SourcePort);
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
