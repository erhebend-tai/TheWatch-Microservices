using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TheWatch.P2.VoiceEmergency.Services;

/// <summary>
/// Result of a 911 emergency call dispatch attempt.
/// </summary>
public record EmergencyCallResult(bool Success, string? CallSid = null, string? Error = null);

/// <summary>
/// Service responsible for contacting 911 on behalf of a user when an emergency is signalled.
/// When a telephony webhook URL is configured (EmergencyCall:WebhookUrl), the service
/// posts the incident details to that endpoint (e.g. a Twilio Studio Flow trigger URL).
/// When no webhook is configured the call is logged at Critical level so that on-call
/// operators are immediately alerted via any log-aggregation pipeline (Splunk, Datadog, etc.).
/// </summary>
public interface IEmergencyCallService
{
    /// <summary>
    /// Dispatch a 911 emergency call for the given incident.
    /// </summary>
    Task<EmergencyCallResult> Dispatch911Async(
        Guid incidentId,
        string description,
        double latitude,
        double longitude,
        string? callerPhone = null,
        CancellationToken ct = default);
}

public class EmergencyCallService : IEmergencyCallService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmergencyCallService> _logger;

    public EmergencyCallService(
        IHttpClientFactory httpFactory,
        IConfiguration configuration,
        ILogger<EmergencyCallService> logger)
    {
        _httpFactory = httpFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<EmergencyCallResult> Dispatch911Async(
        Guid incidentId,
        string description,
        double latitude,
        double longitude,
        string? callerPhone = null,
        CancellationToken ct = default)
    {
        _logger.LogCritical(
            "911 DISPATCH — IncidentId={IncidentId} Lat={Latitude} Lon={Longitude} Caller={CallerPhone} Description={Description}",
            incidentId, latitude, longitude, callerPhone ?? "unknown", description);

        var webhookUrl = _configuration["EmergencyCall:WebhookUrl"];
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            // No telephony provider configured; the critical log above is the fallback
            // so that operators are alerted through the log-aggregation pipeline.
            _logger.LogWarning(
                "EmergencyCall:WebhookUrl is not configured. 911 dispatch logged only. IncidentId={IncidentId}",
                incidentId);
            return new EmergencyCallResult(true);
        }

        try
        {
            var client = _httpFactory.CreateClient("emergency-call");
            var payload = new
            {
                incidentId = incidentId.ToString(),
                description,
                latitude,
                longitude,
                callerPhone,
                dispatchedAtUtc = DateTime.UtcNow.ToString("O")
            };

            var response = await client.PostAsJsonAsync(webhookUrl, payload, ct);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "911 webhook acknowledged. IncidentId={IncidentId} Status={Status}",
                    incidentId, (int)response.StatusCode);
                var body = await response.Content.ReadAsStringAsync(ct);
                return new EmergencyCallResult(true, CallSid: body);
            }

            _logger.LogError(
                "911 webhook returned non-success. IncidentId={IncidentId} Status={Status}",
                incidentId, (int)response.StatusCode);
            return new EmergencyCallResult(false, Error: $"webhook-http-{(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reach 911 webhook. IncidentId={IncidentId}", incidentId);
            return new EmergencyCallResult(false, Error: ex.Message);
        }
    }
}
