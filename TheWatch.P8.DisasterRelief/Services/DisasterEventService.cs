using Microsoft.EntityFrameworkCore;
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
    private readonly IWatchRepository<DisasterEvent> _events;
    private readonly IWatchRepository<EvacuationRoute> _routes;

    public DisasterEventService(IWatchRepository<DisasterEvent> events, IWatchRepository<EvacuationRoute> routes)
    {
        _events = events;
        _routes = routes;
    }

    public async Task<DisasterEvent> CreateAsync(CreateDisasterEventRequest request)
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

        return await _events.AddAsync(evt);
    }

    public async Task<DisasterEvent?> GetByIdAsync(Guid id)
    {
        return await _events.GetByIdAsync(id);
    }

    public async Task<DisasterEventListResponse> ListAsync(int page, int pageSize, EventStatus? status)
    {
        var query = _events.Query();

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new DisasterEventListResponse(items, total, page, pageSize);
    }

    public async Task<DisasterEvent?> UpdateStatusAsync(Guid id, UpdateEventStatusRequest request)
    {
        var evt = await _events.GetByIdAsync(id);
        if (evt is null) return null;

        evt.Status = request.Status;
        evt.UpdatedAt = DateTime.UtcNow;
        await _events.UpdateAsync(evt);
        return evt;
    }

    public async Task<List<EvacuationRoute>> GetRoutesAsync(Guid eventId)
    {
        return await _routes.Query()
            .Where(r => r.DisasterEventId == eventId && r.IsActive)
            .OrderBy(r => r.EstimatedTimeMinutes)
            .ToListAsync();
    }

    public async Task<EvacuationRoute> AddRouteAsync(CreateEvacuationRouteRequest request)
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

        return await _routes.AddAsync(route);
    }

    public async Task ArchiveResolvedEventsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var toArchive = await _events.Query()
            .Where(e => e.Status == EventStatus.Resolved && e.UpdatedAt < cutoff)
            .ToListAsync();

        foreach (var evt in toArchive)
        {
            evt.Status = EventStatus.Archived;
            evt.UpdatedAt = DateTime.UtcNow;
            await _events.UpdateAsync(evt);
        }
    }
}
