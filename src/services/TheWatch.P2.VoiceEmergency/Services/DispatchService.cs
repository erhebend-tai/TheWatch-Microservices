using Microsoft.EntityFrameworkCore;
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
    private readonly IWatchRepository<Dispatch> _dispatches;
    private readonly INotificationService _notifications;
    private readonly ILogger<DispatchService> _logger;

    public DispatchService(IWatchRepository<Dispatch> dispatches, INotificationService notifications, ILogger<DispatchService> logger)
    {
        _dispatches = dispatches;
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

        await _dispatches.AddAsync(dispatch);

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

    public async Task<Dispatch?> GetDispatchAsync(Guid id)
    {
        return await _dispatches.GetByIdAsync(id);
    }

    public async Task<Dispatch?> ExpandRadiusAsync(Guid id, ExpandRadiusRequest request)
    {
        var dispatch = await _dispatches.GetByIdAsync(id);
        if (dispatch is null) return null;

        dispatch.RadiusKm += request.AdditionalKm;
        dispatch.UpdatedAt = DateTime.UtcNow;
        await _dispatches.UpdateAsync(dispatch);

        return dispatch;
    }

    public async Task<List<Dispatch>> GetDispatchesForIncidentAsync(Guid incidentId)
    {
        return await _dispatches.Query()
            .Where(d => d.IncidentId == incidentId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> EscalateUnacknowledgedAsync(TimeSpan timeout)
    {
        var cutoff = DateTime.UtcNow - timeout;
        var toEscalate = await _dispatches.Query()
            .Where(d => d.Status == DispatchStatus.Pending && d.CreatedAt < cutoff)
            .ToListAsync();

        foreach (var dispatch in toEscalate)
        {
            dispatch.Status = DispatchStatus.Escalated;
            dispatch.EscalationCount++;
            dispatch.UpdatedAt = DateTime.UtcNow;
            await _dispatches.UpdateAsync(dispatch);
        }

        return toEscalate.Count;
    }
}
