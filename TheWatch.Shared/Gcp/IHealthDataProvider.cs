namespace TheWatch.Shared.Gcp;

/// <summary>
/// FHIR health data interoperability provider interface (Item 135).
/// Used by P7 FamilyHealth and P9 DoctorServices for standards-based
/// health record exchange.
///
/// Implementations:
///   - NoOpHealthDataProvider: development/testing (returns empty results)
///   - GoogleHealthcareProvider: Google Healthcare API with FHIR R4 (implement in batch)
///
/// Toggle via Gcp:UseHealthcareApi = true in appsettings.json.
/// </summary>
public interface IHealthDataProvider
{
    // ─── Patient Resources ───

    /// <summary>
    /// Create or update a FHIR Patient resource.
    /// </summary>
    Task<FhirResourceResult> UpsertPatientAsync(
        FhirPatient patient, CancellationToken ct = default);

    /// <summary>
    /// Retrieve a FHIR Patient by ID.
    /// </summary>
    Task<FhirPatient?> GetPatientAsync(
        string patientId, CancellationToken ct = default);

    /// <summary>
    /// Search patients by criteria.
    /// </summary>
    Task<List<FhirPatient>> SearchPatientsAsync(
        FhirSearchParams searchParams, CancellationToken ct = default);

    // ─── Observation Resources (vitals, lab results) ───

    /// <summary>
    /// Record a FHIR Observation (vital sign, lab result, etc.).
    /// </summary>
    Task<FhirResourceResult> CreateObservationAsync(
        FhirObservation observation, CancellationToken ct = default);

    /// <summary>
    /// Get observations for a patient, optionally filtered by code/date.
    /// </summary>
    Task<List<FhirObservation>> GetObservationsAsync(
        string patientId, string? code = null, DateTime? since = null,
        CancellationToken ct = default);

    // ─── Encounter Resources (appointments, telehealth) ───

    /// <summary>
    /// Create a FHIR Encounter (doctor visit, telehealth session).
    /// </summary>
    Task<FhirResourceResult> CreateEncounterAsync(
        FhirEncounter encounter, CancellationToken ct = default);

    // ─── Bulk Operations ───

    /// <summary>
    /// Export patient data in FHIR bulk export format (NDJSON).
    /// </summary>
    Task<FhirExportResult> ExportPatientDataAsync(
        string patientId, CancellationToken ct = default);

    /// <summary>
    /// Whether the provider is configured and ready.
    /// </summary>
    bool IsConfigured { get; }
}

// ─── FHIR DTOs (simplified — map to full FHIR R4 resources in implementation) ───

public record FhirPatient
{
    public string? Id { get; init; }
    public string FamilyName { get; init; } = string.Empty;
    public string GivenName { get; init; } = string.Empty;
    public DateTime? BirthDate { get; init; }
    public string? Gender { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public FhirAddress? Address { get; init; }
    public Dictionary<string, string> Identifiers { get; init; } = [];
}

public record FhirAddress(string? Line, string? City, string? State, string? PostalCode, string? Country);

public record FhirObservation
{
    public string? Id { get; init; }
    public string PatientId { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string CodeSystem { get; init; } = "http://loinc.org";
    public string DisplayName { get; init; } = string.Empty;
    public decimal? ValueQuantity { get; init; }
    public string? ValueUnit { get; init; }
    public string? ValueString { get; init; }
    public DateTime EffectiveDateTime { get; init; } = DateTime.UtcNow;
    public string Status { get; init; } = "final";
}

public record FhirEncounter
{
    public string? Id { get; init; }
    public string PatientId { get; init; } = string.Empty;
    public string PractitionerId { get; init; } = string.Empty;
    public string EncounterClass { get; init; } = "AMB"; // AMB, EMER, VR (virtual)
    public string Status { get; init; } = "planned";
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public string? ReasonCode { get; init; }
}

public record FhirResourceResult(bool Success, string? ResourceId = null, string? Error = null);

public record FhirExportResult(bool Success, string? ExportUrl = null, string? Error = null);

public record FhirSearchParams
{
    public string? Name { get; init; }
    public string? Identifier { get; init; }
    public DateTime? BirthDate { get; init; }
    public string? Gender { get; init; }
    public int MaxResults { get; init; } = 20;
}
