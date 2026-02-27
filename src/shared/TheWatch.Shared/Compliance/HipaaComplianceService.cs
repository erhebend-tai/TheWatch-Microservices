using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Compliance;

/// <summary>
/// HIPAA-compliant data handling for P7 (FamilyHealth) and P9 (DoctorServices).
/// Provides: encryption at rest, field-level PHI redaction, access logging,
/// and BAA tracking. All health records pass through this service before storage/retrieval.
/// </summary>
public interface IHipaaComplianceService
{
    /// <summary>Encrypt PHI data for storage at rest (AES-256-GCM).</summary>
    string EncryptPhi(string plainText);

    /// <summary>Decrypt PHI data for authorized access.</summary>
    string DecryptPhi(string cipherText);

    /// <summary>Log a PHI access event (required by HIPAA §164.312(b)).</summary>
    Task LogAccessAsync(PhiAccessEvent accessEvent);

    /// <summary>Redact PHI fields from an object for audit/logging purposes.</summary>
    string RedactPhi<T>(T record, IReadOnlySet<string>? fieldsToRedact = null);

    /// <summary>Verify that a BAA is on file for a given organization.</summary>
    Task<bool> VerifyBaaAsync(string organizationId);
}

public record PhiAccessEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string UserRole { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;  // Read, Write, Delete, Export
    public string ResourceType { get; init; } = string.Empty;  // VitalReading, Appointment, etc.
    public Guid ResourceId { get; init; }
    public string? Reason { get; init; }
    public string IpAddress { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool AccessGranted { get; init; } = true;
}

/// <summary>Business Associate Agreement tracking.</summary>
public record BaaRecord
{
    public string OrganizationId { get; init; } = string.Empty;
    public string OrganizationName { get; init; } = string.Empty;
    public DateTime SignedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string SignedBy { get; init; } = string.Empty;
    public bool IsActive => DateTime.UtcNow < ExpiresAt;
}

/// <summary>PHI field identifiers per HIPAA Safe Harbor de-identification (§164.514).</summary>
public static class PhiFields
{
    public static readonly IReadOnlySet<string> IdentifiableFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Name", "Email", "Phone", "Address", "DateOfBirth", "SocialSecurityNumber",
        "MedicalRecordNumber", "HealthPlanBeneficiaryNumber", "AccountNumber",
        "CertificateLicenseNumber", "VehicleIdentifier", "DeviceIdentifier",
        "WebUrl", "IpAddress", "BiometricIdentifier", "FullFacePhotograph"
    };
}

public class HipaaComplianceService : IHipaaComplianceService
{
    private readonly ILogger<HipaaComplianceService> _logger;
    private readonly byte[] _encryptionKey;

    // PHI access audit log (in production: write to tamper-evident store)
    private readonly List<PhiAccessEvent> _auditLog = new();
    private readonly Dictionary<string, BaaRecord> _baaRegistry = new();

    public HipaaComplianceService(ILogger<HipaaComplianceService> logger, string? encryptionKeyBase64 = null)
    {
        _logger = logger;

        // In production: load from Azure Key Vault / HSM
        if (encryptionKeyBase64 is not null)
        {
            _encryptionKey = Convert.FromBase64String(encryptionKeyBase64);
        }
        else
        {
            _encryptionKey = new byte[32]; // 256-bit key
            RandomNumberGenerator.Fill(_encryptionKey);
            _logger.LogWarning("HIPAA: Using auto-generated encryption key. Configure a persistent key for production.");
        }
    }

    public string EncryptPhi(string plainText)
    {
        var nonce = new byte[12]; // 96-bit nonce for AES-GCM
        RandomNumberGenerator.Fill(nonce);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16]; // 128-bit auth tag

        using var aes = new AesGcm(_encryptionKey, 16);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Format: base64(nonce + tag + cipher)
        var combined = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(combined);
    }

    public string DecryptPhi(string cipherText)
    {
        var combined = Convert.FromBase64String(cipherText);

        var nonce = new byte[12];
        var tag = new byte[16];
        var cipherBytes = new byte[combined.Length - 28];

        Buffer.BlockCopy(combined, 0, nonce, 0, 12);
        Buffer.BlockCopy(combined, 12, tag, 0, 16);
        Buffer.BlockCopy(combined, 28, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];
        using var aes = new AesGcm(_encryptionKey, 16);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public Task LogAccessAsync(PhiAccessEvent accessEvent)
    {
        _auditLog.Add(accessEvent);
        _logger.LogInformation(
            "HIPAA PHI access: user={UserId} action={Action} resource={ResourceType}/{ResourceId} granted={Granted}",
            accessEvent.UserId, accessEvent.Action, accessEvent.ResourceType, accessEvent.ResourceId, accessEvent.AccessGranted);
        return Task.CompletedTask;
    }

    public string RedactPhi<T>(T record, IReadOnlySet<string>? fieldsToRedact = null)
    {
        var redactFields = fieldsToRedact ?? PhiFields.IdentifiableFields;
        var json = JsonSerializer.Serialize(record);
        var doc = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();

        foreach (var key in doc.Keys.ToList())
        {
            if (redactFields.Contains(key))
            {
                doc[key] = "[REDACTED]";
            }
        }

        return JsonSerializer.Serialize(doc);
    }

    public Task<bool> VerifyBaaAsync(string organizationId)
    {
        if (_baaRegistry.TryGetValue(organizationId, out var baa))
        {
            return Task.FromResult(baa.IsActive);
        }
        _logger.LogWarning("HIPAA: No BAA on file for organization {OrgId}", organizationId);
        return Task.FromResult(false);
    }
}
