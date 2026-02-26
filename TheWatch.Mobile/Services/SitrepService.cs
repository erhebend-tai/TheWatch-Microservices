using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Structures speech-to-text output into the SITREP framework:
/// Situation, Incident, Terrain, Resources, Evacuation, Personnel.
/// Supports both voice dictation and manual input for incident reporting.
/// </summary>
public class SitrepService
{
    private readonly SpeechListenerService _speech;
    private readonly ILogger<SitrepService> _logger;

    public event Action<string>? OnDictationResult;

    public SitrepService(SpeechListenerService speech, ILogger<SitrepService> logger)
    {
        _speech = speech;
        _logger = logger;
    }

    /// <summary>Start dictation mode for a specific SITREP section.</summary>
    public async Task StartDictationAsync()
    {
        _speech.OnSpeechRecognized += OnSpeechResult;
        await _speech.StartListeningAsync();
    }

    /// <summary>Stop dictation mode.</summary>
    public async Task StopDictationAsync()
    {
        _speech.OnSpeechRecognized -= OnSpeechResult;
        await _speech.StopListeningAsync();
    }

    private void OnSpeechResult(string text)
    {
        OnDictationResult?.Invoke(text);
    }

    /// <summary>
    /// Parse free-text into SITREP sections based on keyword detection.
    /// Returns a SitrepReport with sections auto-populated where possible.
    /// </summary>
    public SitrepReport ParseFromText(string text)
    {
        var report = new SitrepReport();
        var lower = text.ToLowerInvariant();

        // Simple keyword-based section detection
        if (lower.Contains("situation") || lower.Contains("what happened"))
            report.Situation = text;
        else if (lower.Contains("incident") || lower.Contains("type of"))
            report.Incident = text;
        else if (lower.Contains("terrain") || lower.Contains("location") || lower.Contains("environment"))
            report.Terrain = text;
        else if (lower.Contains("resource") || lower.Contains("need") || lower.Contains("require"))
            report.Resources = text;
        else if (lower.Contains("evacuat") || lower.Contains("exit") || lower.Contains("route"))
            report.Evacuation = text;
        else if (lower.Contains("personnel") || lower.Contains("people") || lower.Contains("victim") || lower.Contains("injur"))
            report.Personnel = text;
        else
            report.Situation = text; // Default to situation

        return report;
    }

    /// <summary>Format a SITREP report into a structured text description.</summary>
    public string FormatReport(SitrepReport report)
    {
        var sections = new List<string>();

        if (!string.IsNullOrWhiteSpace(report.Situation))
            sections.Add($"SITUATION: {report.Situation}");
        if (!string.IsNullOrWhiteSpace(report.Incident))
            sections.Add($"INCIDENT: {report.Incident}");
        if (!string.IsNullOrWhiteSpace(report.Terrain))
            sections.Add($"TERRAIN/ENVIRONMENT: {report.Terrain}");
        if (!string.IsNullOrWhiteSpace(report.Resources))
            sections.Add($"RESOURCES: {report.Resources}");
        if (!string.IsNullOrWhiteSpace(report.Evacuation))
            sections.Add($"EVACUATION: {report.Evacuation}");
        if (!string.IsNullOrWhiteSpace(report.Personnel))
            sections.Add($"PERSONNEL: {report.Personnel}");

        return string.Join("\n\n", sections);
    }
}

public class SitrepReport
{
    public string Situation { get; set; } = "";
    public string Incident { get; set; } = "";
    public string Terrain { get; set; } = "";
    public string Resources { get; set; } = "";
    public string Evacuation { get; set; } = "";
    public string Personnel { get; set; } = "";
    public Guid? IncidentId { get; set; }
    public List<Guid> AttachedEvidenceIds { get; set; } = [];
}
