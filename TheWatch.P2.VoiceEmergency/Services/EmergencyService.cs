using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TheWatch.P2.VoiceEmergency.Emergency;
using TheWatch.Shared.Notifications;

namespace TheWatch.P2.VoiceEmergency.Services;

public interface IEmergencyService
{
    Task<Incident> CreateIncidentAsync(CreateIncidentRequest request);
    Task<Incident?> GetIncidentAsync(Guid id);
    Task<IncidentListResponse> ListIncidentsAsync(int page = 1, int pageSize = 20, IncidentStatus? statusFilter = null, EmergencyType? typeFilter = null);
    Task<Incident?> UpdateStatusAsync(Guid id, UpdateIncidentStatusRequest request);
    Task<int> ArchiveResolvedAsync(TimeSpan olderThan);
}

public class EmergencyService : IEmergencyService
{
    private readonly ConcurrentDictionary<Guid, Incident> _incidents = new();
    private readonly INotificationService _notifications;
    private readonly ILogger<EmergencyService> _logger;

    public EmergencyService(INotificationService notifications, ILogger<EmergencyService> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<Incident> CreateIncidentAsync(CreateIncidentRequest request)
    {
        var incident = new Incident
        {
            Type = request.Type,
            Description = request.Description,
            Location = request.Location,
            ReporterId = request.ReporterId,
            ReporterName = request.ReporterName,
            ReporterPhone = request.ReporterPhone,
            Severity = request.Severity,
            Tags = request.Tags ?? []
        };

        if (!_incidents.TryAdd(incident.Id, incident))
            throw new InvalidOperationException("Failed to create incident.");

        // Push notification to emergency topic
        var priority = incident.Severity >= 4 ? NotificationPriority.Critical : NotificationPriority.High;
        var notification = NotificationSetup.CreateNotification(
            $"🚨 {incident.Type} Reported",
            incident.Description.Length > 100
                ? incident.Description[..100] + "..."
                : incident.Description,
            priority,
            new Dictionary<string, string>
            {
                ["incidentId"] = incident.Id.ToString(),
                ["type"] = incident.Type.ToString(),
                ["severity"] = incident.Severity.ToString(),
                ["lat"] = incident.Location.Latitude.ToString("F6"),
                ["lon"] = incident.Location.Longitude.ToString("F6")
            });

        _ = _notifications.SendToTopicAsync(NotificationSetup.DefaultTopic, notification)
            .ContinueWith(t =>
            {
                if (!t.Result.Success)
                    _logger.LogWarning("Failed to send incident notification: {Error}", t.Result.Error);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

        return incident;
    }

    public Task<Incident?> GetIncidentAsync(Guid id)
    {
        _incidents.TryGetValue(id, out var incident);
        return Task.FromResult(incident);
    }

    public Task<IncidentListResponse> ListIncidentsAsync(
        int page = 1, int pageSize = 20,
        IncidentStatus? statusFilter = null,
        EmergencyType? typeFilter = null)
    {
        var query = _incidents.Values.AsEnumerable();

        if (statusFilter.HasValue)
            query = query.Where(i => i.Status == statusFilter.Value);
        if (typeFilter.HasValue)
            query = query.Where(i => i.Type == typeFilter.Value);

        var ordered = query.OrderByDescending(i => i.CreatedAt).ToList();
        var totalCount = ordered.Count;
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new IncidentListResponse(items, totalCount, page, pageSize));
    }

    public Task<Incident?> UpdateStatusAsync(Guid id, UpdateIncidentStatusRequest request)
    {
        if (!_incidents.TryGetValue(id, out var incident))
            return Task.FromResult<Incident?>(null);

        incident.Status = request.Status;
        incident.UpdatedAt = DateTime.UtcNow;

        if (request.Status == IncidentStatus.Resolved)
            incident.ResolvedAt = DateTime.UtcNow;

        return Task.FromResult<Incident?>(incident);
    }

    public Task<int> ArchiveResolvedAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var toArchive = _incidents.Values
            .Where(i => i.Status == IncidentStatus.Resolved && i.ResolvedAt < cutoff)
            .ToList();

        foreach (var incident in toArchive)
        {
            incident.Status = IncidentStatus.Archived;
            incident.UpdatedAt = DateTime.UtcNow;
        }

        return Task.FromResult(toArchive.Count);
    }
}
