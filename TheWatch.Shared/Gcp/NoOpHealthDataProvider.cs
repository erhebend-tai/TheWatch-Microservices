using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Gcp;

/// <summary>
/// No-op health data provider for development/testing.
/// Returns empty results. Replace with GoogleHealthcareProvider when implementing.
/// </summary>
public class NoOpHealthDataProvider : IHealthDataProvider
{
    private readonly ILogger<NoOpHealthDataProvider> _logger;

    public NoOpHealthDataProvider(ILogger<NoOpHealthDataProvider> logger)
    {
        _logger = logger;
    }

    public bool IsConfigured => false;

    public Task<FhirResourceResult> UpsertPatientAsync(FhirPatient patient, CancellationToken ct)
    {
        _logger.LogDebug("NoOp FHIR: UpsertPatientAsync for {Name}", patient.GivenName);
        return Task.FromResult(new FhirResourceResult(true, ResourceId: Guid.NewGuid().ToString()));
    }

    public Task<FhirPatient?> GetPatientAsync(string patientId, CancellationToken ct)
    {
        _logger.LogDebug("NoOp FHIR: GetPatientAsync for {Id}", patientId);
        return Task.FromResult<FhirPatient?>(null);
    }

    public Task<List<FhirPatient>> SearchPatientsAsync(FhirSearchParams searchParams, CancellationToken ct)
    {
        _logger.LogDebug("NoOp FHIR: SearchPatientsAsync");
        return Task.FromResult(new List<FhirPatient>());
    }

    public Task<FhirResourceResult> CreateObservationAsync(FhirObservation observation, CancellationToken ct)
    {
        _logger.LogDebug("NoOp FHIR: CreateObservationAsync for patient {PatientId}", observation.PatientId);
        return Task.FromResult(new FhirResourceResult(true, ResourceId: Guid.NewGuid().ToString()));
    }

    public Task<List<FhirObservation>> GetObservationsAsync(
        string patientId, string? code, DateTime? since, CancellationToken ct)
    {
        _logger.LogDebug("NoOp FHIR: GetObservationsAsync for patient {PatientId}", patientId);
        return Task.FromResult(new List<FhirObservation>());
    }

    public Task<FhirResourceResult> CreateEncounterAsync(FhirEncounter encounter, CancellationToken ct)
    {
        _logger.LogDebug("NoOp FHIR: CreateEncounterAsync for patient {PatientId}", encounter.PatientId);
        return Task.FromResult(new FhirResourceResult(true, ResourceId: Guid.NewGuid().ToString()));
    }

    public Task<FhirExportResult> ExportPatientDataAsync(string patientId, CancellationToken ct)
    {
        _logger.LogDebug("NoOp FHIR: ExportPatientDataAsync for {PatientId}", patientId);
        return Task.FromResult(new FhirExportResult(false, Error: "NoOp provider — no export available"));
    }
}
