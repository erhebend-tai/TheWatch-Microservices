using Microsoft.EntityFrameworkCore;
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
    private readonly IWatchRepository<Incident> _incidents;
    private readonly INotificationService _notifications;
    private readonly ILogger<EmergencyService> _logger;

    public EmergencyService(IWatchRepository<Incident> incidents, INotificationService notifications, ILogger<EmergencyService> logger)
    {
        _incidents = incidents;
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

        await _incidents.AddAsync(incident);

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

    public async Task<Incident?> GetIncidentAsync(Guid id)
    {
        return await _incidents.GetByIdAsync(id);
    }

    public async Task<IncidentListResponse> ListIncidentsAsync(
        int page = 1, int pageSize = 20,
        IncidentStatus? statusFilter = null,
        EmergencyType? typeFilter = null)
    {
        var query = _incidents.Query();

        if (statusFilter.HasValue)
            query = query.Where(i => i.Status == statusFilter.Value);
        if (typeFilter.HasValue)
            query = query.Where(i => i.Type == typeFilter.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new IncidentListResponse(items, totalCount, page, pageSize);
    }

    public async Task<Incident?> UpdateStatusAsync(Guid id, UpdateIncidentStatusRequest request)
    {
        var incident = await _incidents.GetByIdAsync(id);
        if (incident is null) return null;

        incident.Status = request.Status;
        incident.UpdatedAt = DateTime.UtcNow;

        if (request.Status == IncidentStatus.Resolved)
            incident.ResolvedAt = DateTime.UtcNow;

        await _incidents.UpdateAsync(incident);
        return incident;
    }

    public async Task<int> ArchiveResolvedAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var toArchive = await _incidents.Query()
            .Where(i => i.Status == IncidentStatus.Resolved && i.ResolvedAt < cutoff)
            .ToListAsync();

        foreach (var incident in toArchive)
        {
            incident.Status = IncidentStatus.Archived;
            incident.UpdatedAt = DateTime.UtcNow;
            await _incidents.UpdateAsync(incident);
        }

        return toArchive.Count;
    }
}
