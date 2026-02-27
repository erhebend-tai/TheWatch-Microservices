using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Services;

/// <summary>
/// Persists ambulance pre-arrival triage intake records so that responders
/// have the caller's symptoms and matched guidance available when they arrive.
/// Intake can be linked to an incident after the SOS is submitted.
/// </summary>
public interface ITriageService
{
    Task<TriageIntake> LogIntakeAsync(LogTriageIntakeRequest request);
    Task<TriageIntake?> GetIntakeAsync(Guid id);
    Task<List<TriageIntake>> GetIntakesForIncidentAsync(Guid incidentId);
}

public class TriageService : ITriageService
{
    private readonly IWatchRepository<TriageIntake> _intakes;
    private readonly ILogger<TriageService> _logger;

    public TriageService(IWatchRepository<TriageIntake> intakes, ILogger<TriageService> logger)
    {
        _intakes = intakes;
        _logger = logger;
    }

    public async Task<TriageIntake> LogIntakeAsync(LogTriageIntakeRequest request)
    {
        var intake = new TriageIntake
        {
            ReporterId = request.ReporterId,
            IncidentId = request.IncidentId,
            Symptoms = request.Symptoms,
            InputMethod = request.InputMethod,
            SubstanceName = request.SubstanceName,
            MatchedGuidance = request.MatchedGuidance,
            TriageSeverity = request.TriageSeverity
        };

        await _intakes.AddAsync(intake);

        _logger.LogInformation(
            "Triage intake logged. Id={IntakeId} ReporterId={ReporterId} InputMethod={InputMethod} Severity={Severity}",
            intake.Id, intake.ReporterId, intake.InputMethod, intake.TriageSeverity);

        return intake;
    }

    public async Task<TriageIntake?> GetIntakeAsync(Guid id)
    {
        return await _intakes.GetByIdAsync(id);
    }

    public async Task<List<TriageIntake>> GetIntakesForIncidentAsync(Guid incidentId)
    {
        return await _intakes.Query()
            .Where(t => t.IncidentId == incidentId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();
    }
}
