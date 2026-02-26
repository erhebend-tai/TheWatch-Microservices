using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Data;

public class AuthIdentityDbContext : IdentityDbContext<WatchUser, WatchRole, Guid>
{
    public AuthIdentityDbContext(DbContextOptions<AuthIdentityDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<DeviceTrust> DeviceTrusts => Set<DeviceTrust>();
    public DbSet<EulaVersion> EulaVersions => Set<EulaVersion>();
    public DbSet<EulaAcceptance> EulaAcceptances => Set<EulaAcceptance>();
    public DbSet<OnboardingProgress> OnboardingProgresses => Set<OnboardingProgress>();
    public DbSet<FidoCredential> FidoCredentials => Set<FidoCredential>();
    public DbSet<ThreatAssessment> ThreatAssessments => Set<ThreatAssessment>();
    public DbSet<AttackDetectionRule> AttackDetectionRules => Set<AttackDetectionRule>();
    public DbSet<BatteryBenchmark> BatteryBenchmarks => Set<BatteryBenchmark>();
    public DbSet<DevicePermission> DevicePermissions => Set<DevicePermission>();
    public DbSet<PermissionRequest> PermissionRequests => Set<PermissionRequest>();
    public DbSet<PermissionAudit> PermissionAudits => Set<PermissionAudit>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // RefreshToken
        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Token).IsUnique();
            e.HasIndex(r => r.UserId);
            e.Property(r => r.Token).HasMaxLength(512);
            e.Property(r => r.ReplacedByToken).HasMaxLength(512);
            e.Property(r => r.DeviceFingerprint).HasMaxLength(256);
            e.Property(r => r.IpAddress).HasMaxLength(45);
        });

        // AuditEvent
        builder.Entity<AuditEvent>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.EventType);
            e.HasIndex(a => a.Timestamp);
            e.Property(a => a.EventType).HasMaxLength(64);
            e.Property(a => a.IpAddress).HasMaxLength(45);
            e.Property(a => a.UserAgent).HasMaxLength(512);
            e.Property(a => a.DeviceFingerprint).HasMaxLength(256);
            e.Property(a => a.FailureReason).HasMaxLength(512);
        });

        // DeviceTrust
        builder.Entity<DeviceTrust>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.UserId);
            e.HasIndex(d => d.Fingerprint);
            e.Property(d => d.Fingerprint).HasMaxLength(256);
            e.Property(d => d.IpAddress).HasMaxLength(45);
            e.Property(d => d.Location).HasMaxLength(256);
        });

        // EulaVersion
        builder.Entity<EulaVersion>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasIndex(v => v.Version).IsUnique();
            e.Property(v => v.Version).HasMaxLength(32);
        });

        // EulaAcceptance
        builder.Entity<EulaAcceptance>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.UserId, a.EulaVersionId }).IsUnique();
        });

        // OnboardingProgress
        builder.Entity<OnboardingProgress>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.UserId).IsUnique();
        });

        // FidoCredential
        builder.Entity<FidoCredential>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => f.UserId);
            e.Property(f => f.CredentialId).HasMaxLength(512);
            e.Property(f => f.AaGuid).HasMaxLength(64);
        });

        // ThreatAssessment
        builder.Entity<ThreatAssessment>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.DetectedAt);
            e.HasIndex(t => t.Severity);
            e.Property(t => t.Category).HasMaxLength(64);
            e.Property(t => t.RuleName).HasMaxLength(128);
        });

        // AttackDetectionRule
        builder.Entity<AttackDetectionRule>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.TechniqueId).IsUnique();
            e.Property(r => r.TechniqueId).HasMaxLength(16);
            e.Property(r => r.TechniqueName).HasMaxLength(128);
            e.Property(r => r.Tactic).HasMaxLength(64);
        });

        // Battery Benchmarks
        builder.Entity<BatteryBenchmark>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DeviceModel).HasMaxLength(200).IsRequired();
            entity.Property(e => e.OsVersion).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TestProfile).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_BatteryBenchmark_Battery", "[StartBatteryPct] BETWEEN 0 AND 100 AND [EndBatteryPct] BETWEEN 0 AND 100");
                t.HasCheckConstraint("CK_BatteryBenchmark_Duration", "[DurationMinutes] > 0");
            });
        });

        // Device Permissions
        builder.Entity<DevicePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.PermissionType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.State).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.HasIndex(e => new { e.DeviceId, e.PermissionType }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.State);
        });

        // Permission Requests
        builder.Entity<PermissionRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.PermissionType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Justification).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.FeatureContext).HasMaxLength(200);
            entity.Property(e => e.Outcome).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(e => new { e.UserId, e.PermissionType });
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.Outcome);
            entity.ToTable(t => t.HasCheckConstraint("CK_PermissionRequest_Prompts", "[PromptCount] >= 1"));
        });

        // Permission Audits
        builder.Entity<PermissionAudit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.PermissionType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.OldState).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.NewState).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ChangeSource).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.HasIndex(e => new { e.DeviceId, e.PermissionType });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ChangedAt).IsDescending();
        });

        // WatchRole enrichment
        builder.Entity<WatchRole>(entity =>
        {
            entity.Property(e => e.AllowedIpRanges).HasMaxLength(2000);
            entity.HasIndex(e => e.RoleLevel);
            entity.HasIndex(e => e.IsSystemRole);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_WatchRole_SessionDuration", "[MaxSessionDurationMinutes] > 0");
            });
        });
    }
}
