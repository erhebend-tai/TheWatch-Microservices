namespace TheWatch.Shared.Security;

/// <summary>
/// Item 293: Data retention policies per entity type.
/// Defines the maximum time data of each category may be retained before it must be
/// purged or anonymised. Policies comply with GDPR Article 5(1)(e), HIPAA 45 CFR §164.530(j),
/// NIST MP-6, and STIG SI-12 requirements.
/// </summary>
/// <remarks>
/// Retention periods (from the creation/last-active date unless otherwise noted):
/// <list type="table">
///   <listheader><term>Category</term><description>Retention period</description></listheader>
///   <item><term>Pii</term><description>Active account lifetime + 30 days after deletion request (GDPR Art.17)</description></item>
///   <item><term>Phi</term><description>6 years (HIPAA 45 CFR §164.530(j))</description></item>
///   <item><term>Evidence</term><description>7 years (legal hold / chain-of-custody)</description></item>
///   <item><term>AuditLog</term><description>1 year minimum (STIG V-222441)</description></item>
///   <item><term>Geolocation</term><description>90 days</description></item>
///   <item><term>SessionToken</term><description>8 hours (matches refresh token lifetime — STIG V-222578)</description></item>
///   <item><term>SmsOtp</term><description>5 minutes (TTL enforced by Redis/cache)</description></item>
///   <item><term>TempFile</term><description>24 hours</description></item>
/// </list>
/// </remarks>
public static class DataRetentionPolicy
{
    /// <summary>PII: active account + 30-day grace after deletion request (GDPR Art.17).</summary>
    public static readonly TimeSpan PiiGracePeriod = TimeSpan.FromDays(30);

    /// <summary>PHI: 6 years per HIPAA 45 CFR §164.530(j).</summary>
    public static readonly TimeSpan Phi = TimeSpan.FromDays(6 * 365);

    /// <summary>Evidence / chain-of-custody: 7 years.</summary>
    public static readonly TimeSpan Evidence = TimeSpan.FromDays(7 * 365);

    /// <summary>Audit logs: 1 year minimum (STIG V-222441).</summary>
    public static readonly TimeSpan AuditLog = TimeSpan.FromDays(365);

    /// <summary>Geolocation data: 90 days.</summary>
    public static readonly TimeSpan Geolocation = TimeSpan.FromDays(90);

    /// <summary>Session / refresh tokens: 8 hours (STIG V-222578).</summary>
    public static readonly TimeSpan SessionToken = TimeSpan.FromHours(8);

    /// <summary>SMS OTP codes: 5 minutes.</summary>
    public static readonly TimeSpan SmsOtp = TimeSpan.FromMinutes(5);

    /// <summary>Temporary upload / scratch files: 24 hours.</summary>
    public static readonly TimeSpan TempFile = TimeSpan.FromHours(24);

    /// <summary>Password age: 60 days maximum before forced change (STIG V-222544).</summary>
    public static readonly TimeSpan PasswordMaxAge = TimeSpan.FromDays(60);

    /// <summary>Password minimum age: 24 hours before next change allowed (STIG V-222545).</summary>
    public static readonly TimeSpan PasswordMinAge = TimeSpan.FromHours(24);
}

/// <summary>
/// Marks an entity as subject to a specific data-retention category so that automated
/// purge jobs can discover and process expirable records without hard-coded queries.
/// </summary>
public interface IRetainedEntity
{
    /// <summary>UTC timestamp when the record was created.</summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// The data retention category that governs when this entity may be hard-deleted.
    /// </summary>
    RetentionCategory RetentionCategory { get; }
}

/// <summary>
/// Data retention categories mapped to <see cref="DataRetentionPolicy"/> periods.
/// </summary>
public enum RetentionCategory
{
    Pii,
    Phi,
    Evidence,
    AuditLog,
    Geolocation,
    SessionToken,
    SmsOtp,
    TempFile
}
