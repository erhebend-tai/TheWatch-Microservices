using System.Collections.Concurrent;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Services;

public interface IDisasterEventService
{
    Task<DisasterEvent> CreateAsync(CreateDisasterEventRequest request);
    Task<DisasterEvent?> GetByIdAsync(Guid id);
    Task<DisasterEventListResponse> ListAsync(int page = 1, int pageSize = 20, EventStatus? status = null);
    Task<DisasterEvent?> UpdateStatusAsync(Guid id, UpdateEventStatusRequest request);
    Task<List<EvacuationRoute>> GetRoutesAsync(Guid eventId);
    Task<EvacuationRoute> AddRouteAsync(CreateEvacuationRouteRequest request);
    Task ArchiveResolvedEventsAsync(TimeSpan olderThan);
}

public class DisasterEventService : IDisasterEventService
{
    private readonly ConcurrentDictionary<Guid, DisasterEvent> _events = new();
    private readonly ConcurrentDictionary<Guid, EvacuationRoute> _routes = new();

    public Task<DisasterEvent> CreateAsync(CreateDisasterEventRequest request)
    {
        var evt = new DisasterEvent
        {
            Type = request.Type,
            Name = request.Name,
            Description = request.Description,
            Location = new GeoPoint(request.Latitude, request.Longitude),
            RadiusKm = request.RadiusKm,
            Severity = request.Severity
        };

        _events[evt.Id] = evt;
        return Task.FromResult(evt);
    }

    public Task<DisasterEvent?> GetByIdAsync(Guid id)
    {
        _events.TryGetValue(id, out var evt);
        return Task.FromResult(evt);
    }

    public Task<DisasterEventListResponse> ListAsync(int page, int pageSize, EventStatus? status)
    {
        var query = _events.Values.AsEnumerable();

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var total = query.Count();
        var items = query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new DisasterEventListResponse(items, total, page, pageSize));
    }

    public Task<DisasterEvent?> UpdateStatusAsync(Guid id, UpdateEventStatusRequest request)
    {
        if (!_events.TryGetValue(id, out var evt))
            return Task.FromResult<DisasterEvent?>(null);

        evt.Status = request.Status;
        evt.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult<DisasterEvent?>(evt);
    }

    public Task<List<EvacuationRoute>> GetRoutesAsync(Guid eventId)
    {
        var routes = _routes.Values
            .Where(r => r.DisasterEventId == eventId && r.IsActive)
            .OrderBy(r => r.EstimatedTimeMinutes)
            .ToList();
        return Task.FromResult(routes);
    }

    public Task<EvacuationRoute> AddRouteAsync(CreateEvacuationRouteRequest request)
    {
        var route = new EvacuationRoute
        {
            DisasterEventId = request.DisasterEventId,
            Origin = new GeoPoint(request.OriginLat, request.OriginLon),
            Destination = new GeoPoint(request.DestLat, request.DestLon),
            DistanceKm = request.DistanceKm,
            EstimatedTimeMinutes = request.EstimatedTimeMinutes,
            Description = request.Description
        };

        _routes[route.Id] = route;
        return Task.FromResult(route);
    }

    public Task ArchiveResolvedEventsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        foreach (var evt in _events.Values.Where(e => e.Status == EventStatus.Resolved && e.UpdatedAt < cutoff))
        {
            evt.Status = EventStatus.Archived;
            evt.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }
}
