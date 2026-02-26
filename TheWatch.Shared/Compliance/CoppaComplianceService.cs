using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Compliance;

/// <summary>
/// COPPA compliance for P7 FamilyHealth child data.
/// Enforces: verifiable parental consent, data minimization for children under 13,
/// parental access/deletion rights, and data retention limits.
/// </summary>
public interface ICoppaComplianceService
{
    /// <summary>Check if a member is a child requiring COPPA protections (under 13).</summary>
    bool RequiresCoppaProtection(DateTime? dateOfBirth);

    /// <summary>Verify that parental consent exists for a child member.</summary>
    Task<ConsentStatus> VerifyParentalConsentAsync(Guid childMemberId, Guid familyGroupId);

    /// <summary>Record parental consent for a child member.</summary>
    Task<bool> RecordConsentAsync(ParentalConsentRecord consent);

    /// <summary>Revoke consent — triggers data minimization/deletion.</summary>
    Task<bool> RevokeConsentAsync(Guid childMemberId, Guid parentId);

    /// <summary>Apply data minimization — strip non-essential fields from child records.</summary>
    T MinimizeChildData<T>(T record) where T : class;

    /// <summary>Check if data retention limit has been exceeded for child data.</summary>
    bool IsRetentionExpired(DateTime createdAt, int maxRetentionDays = 365);
}

public record ParentalConsentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ChildMemberId { get; init; }
    public Guid ParentUserId { get; init; }
    public Guid FamilyGroupId { get; init; }
    public ConsentType Type { get; init; }
    public string ParentEmail { get; init; } = string.Empty;
    public string VerificationMethod { get; init; } = string.Empty; // email, credit_card, video_call
    public bool IsVerified { get; init; }
    public DateTime ConsentedAt { get; init; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; init; }
    public bool IsActive => RevokedAt is null;
}

public enum ConsentType
{
    DataCollection,      // Consent to collect child's data
    LocationTracking,    // Consent to track child's location
    HealthMonitoring,    // Consent to monitor child's vitals
    PhotoCapture,        // Consent for evidence photo collection
    ThirdPartySharing    // Consent to share with emergency services
}

public record ConsentStatus
{
    public bool HasConsent { get; init; }
    public bool IsVerified { get; init; }
    public IReadOnlyList<ConsentType> GrantedTypes { get; init; } = [];
    public DateTime? ConsentedAt { get; init; }
    public Guid? ParentUserId { get; init; }
}

public class CoppaComplianceService : ICoppaComplianceService
{
    private readonly ILogger<CoppaComplianceService> _logger;
    private readonly Dictionary<Guid, List<ParentalConsentRecord>> _consentStore = new();

    private const int CoppaAgeLimit = 13;
    private const int DefaultRetentionDays = 365;

    // Fields that are non-essential for children and should be minimized
    private static readonly HashSet<string> NonEssentialChildFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Email", "Phone", "Address", "SocialSecurityNumber", "IpAddress",
        "DeviceIdentifier", "FullFacePhotograph", "BiometricIdentifier"
    };

    public CoppaComplianceService(ILogger<CoppaComplianceService> logger)
    {
        _logger = logger;
    }

    public bool RequiresCoppaProtection(DateTime? dateOfBirth)
    {
        if (dateOfBirth is null) return false;
        var age = DateTime.UtcNow.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > DateTime.UtcNow.AddYears(-age)) age--;
        return age < CoppaAgeLimit;
    }

    public Task<ConsentStatus> VerifyParentalConsentAsync(Guid childMemberId, Guid familyGroupId)
    {
        if (!_consentStore.TryGetValue(childMemberId, out var records))
        {
            return Task.FromResult(new ConsentStatus { HasConsent = false });
        }

        var activeConsents = records.Where(r => r.IsActive && r.FamilyGroupId == familyGroupId).ToList();
        if (activeConsents.Count == 0)
        {
            return Task.FromResult(new ConsentStatus { HasConsent = false });
        }

        return Task.FromResult(new ConsentStatus
        {
            HasConsent = true,
            IsVerified = activeConsents.All(c => c.IsVerified),
            GrantedTypes = activeConsents.Select(c => c.Type).Distinct().ToList(),
            ConsentedAt = activeConsents.Min(c => c.ConsentedAt),
            ParentUserId = activeConsents.First().ParentUserId
        });
    }

    public Task<bool> RecordConsentAsync(ParentalConsentRecord consent)
    {
        if (!_consentStore.TryGetValue(consent.ChildMemberId, out var records))
        {
            records = new List<ParentalConsentRecord>();
            _consentStore[consent.ChildMemberId] = records;
        }

        records.Add(consent);
        _logger.LogInformation(
            "COPPA consent recorded: child={ChildId} parent={ParentId} type={Type} verified={Verified}",
            consent.ChildMemberId, consent.ParentUserId, consent.Type, consent.IsVerified);

        return Task.FromResult(true);
    }

    public Task<bool> RevokeConsentAsync(Guid childMemberId, Guid parentId)
    {
        if (!_consentStore.TryGetValue(childMemberId, out var records))
        {
            return Task.FromResult(false);
        }

        var revoked = 0;
        foreach (var record in records.Where(r => r.ParentUserId == parentId && r.IsActive))
        {
            // Records are immutable — we create a new version with RevokedAt set
            var index = records.IndexOf(record);
            records[index] = record with { RevokedAt = DateTime.UtcNow };
            revoked++;
        }

        if (revoked > 0)
        {
            _logger.LogWarning("COPPA consent revoked: child={ChildId} parent={ParentId} count={Count}",
                childMemberId, parentId, revoked);
        }

        return Task.FromResult(revoked > 0);
    }

    public T MinimizeChildData<T>(T record) where T : class
    {
        // Use reflection to null out non-essential fields
        var type = typeof(T);
        foreach (var prop in type.GetProperties())
        {
            if (NonEssentialChildFields.Contains(prop.Name) && prop.CanWrite)
            {
                if (prop.PropertyType == typeof(string))
                    prop.SetValue(record, null);
                else if (Nullable.GetUnderlyingType(prop.PropertyType) is not null)
                    prop.SetValue(record, null);
            }
        }
        return record;
    }

    public bool IsRetentionExpired(DateTime createdAt, int maxRetentionDays = DefaultRetentionDays) =>
        DateTime.UtcNow > createdAt.AddDays(maxRetentionDays);
}
