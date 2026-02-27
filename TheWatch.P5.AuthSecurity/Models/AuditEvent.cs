namespace TheWatch.P5.AuthSecurity.Models;

public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    /// <summary>Attempted user identity (email/username) — captured even on failed logins.</summary>
    public string? AttemptedIdentity { get; set; }
    public string? IpAddress { get; set; }
    /// <summary>Source port of the remote connection — STIG V-222441 requirement.</summary>
    public int? SourcePort { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceFingerprint { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? AdditionalData { get; set; }
}
