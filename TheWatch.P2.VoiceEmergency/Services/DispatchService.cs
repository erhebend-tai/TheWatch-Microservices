using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TheWatch.P2.VoiceEmergency.Emergency;
using TheWatch.Shared.Notifications;

namespace TheWatch.P2.VoiceEmergency.Services;

public interface IDispatchService
{
    Task<Dispatch> CreateDispatchAsync(CreateDispatchRequest request);
    Task<Dispatch?> GetDispatchAsync(Guid id);
    Task<Dispatch?> ExpandRadiusAsync(Guid id, ExpandRadiusRequest request);
    Task<List<Dispatch>> GetDispatchesForIncidentAsync(Guid incidentId);
    Task<int> EscalateUnacknowledgedAsync(TimeSpan timeout);
}

public class DispatchService : IDispatchService
{
    private readonly ConcurrentDictionary<Guid, Dispatch> _dispatches = new();
    private readonly INotificationService _notifications;
    private readonly ILogger<DispatchService> _logger;

    public DispatchService(INotificationService notifications, ILogger<DispatchService> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<Dispatch> CreateDispatchAsync(CreateDispatchRequest request)
    {
        var dispatch = new Dispatch
        {
            IncidentId = request.IncidentId,
            RadiusKm = request.RadiusKm,
            RespondersRequested = request.RespondersRequested
        };

        if (!_dispatches.TryAdd(dispatch.Id, dispatch))
            throw new InvalidOperationException("Failed to create dispatch.");

        // Notify responders about new dispatch
        var notification = NotificationSetup.CreateNotification(
            "Dispatch Requested",
            $"Responders needed within {dispatch.RadiusKm:F1}km — {dispatch.RespondersRequested} requested",
            NotificationPriority.Critical,
            new Dictionary<string, string>
            {
                ["dispatchId"] = dispatch.Id.ToString(),
                ["incidentId"] = dispatch.IncidentId.ToString(),
                ["radiusKm"] = dispatch.RadiusKm.ToString("F1")
            });

        _ = _notifications.SendToTopicAsync("watch-dispatch", notification)
            .ContinueWith(t =>
            {
                if (!t.Result.Success)
                    _logger.LogWarning("Failed to send dispatch notification: {Error}", t.Result.Error);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

        return dispatch;
    }

    public Task<Dispatch?> GetDispatchAsync(Guid id)
    {
        _dispatches.TryGetValue(id, out var dispatch);
        return Task.FromResult(dispatch);
    }

    public Task<Dispatch?> ExpandRadiusAsync(Guid id, ExpandRadiusRequest request)
    {
        if (!_dispatches.TryGetValue(id, out var dispatch))
            return Task.FromResult<Dispatch?>(null);

        dispatch.RadiusKm += request.AdditionalKm;
        dispatch.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<Dispatch?>(dispatch);
    }

    public Task<List<Dispatch>> GetDispatchesForIncidentAsync(Guid incidentId)
    {
        var dispatches = _dispatches.Values
            .Where(d => d.IncidentId == incidentId)
            .OrderByDescending(d => d.CreatedAt)
            .ToList();

        return Task.FromResult(dispatches);
    }

    public Task<int> EscalateUnacknowledgedAsync(TimeSpan timeout)
    {
        var cutoff = DateTime.UtcNow - timeout;
        var toEscalate = _dispatches.Values
            .Where(d => d.Status == DispatchStatus.Pending && d.CreatedAt < cutoff)
            .ToList();

        foreach (var dispatch in toEscalate)
        {
            dispatch.Status = DispatchStatus.Escalated;
            dispatch.EscalationCount++;
            dispatch.UpdatedAt = DateTime.UtcNow;
        }

        return Task.FromResult(toEscalate.Count);
    }
}
