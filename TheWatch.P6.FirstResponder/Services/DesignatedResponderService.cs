using Microsoft.EntityFrameworkCore;
using TheWatch.P6.FirstResponder.Responders;

namespace TheWatch.P6.FirstResponder.Services;

public interface IDesignatedResponderService
{
    Task<DesignatedResponder> SignupAsync(SignupDesignatedResponderRequest request);
    Task<DesignatedResponder?> GetByIdAsync(Guid id);
    Task<DesignatedResponderListResponse> ListAsync(int page = 1, int pageSize = 20, DesignatedResponderStatus? status = null);
    Task<DesignatedResponder?> UpdateStatusAsync(Guid id, UpdateDesignatedResponderStatusRequest request);
    Task<List<DesignatedResponderMapItem>> GetMapItemsAsync(DesignatedResponderStatus? status = null);
    Task ActivateScheduledRespondersAsync();
}

public class DesignatedResponderService : IDesignatedResponderService
{
    private readonly IWatchRepository<DesignatedResponder> _repository;

    public DesignatedResponderService(IWatchRepository<DesignatedResponder> repository)
    {
        _repository = repository;
    }

    public async Task<DesignatedResponder> SignupAsync(SignupDesignatedResponderRequest request)
    {
        var responder = new DesignatedResponder
        {
            VolunteerName = request.VolunteerName,
            Email = request.Email,
            Phone = request.Phone,
            Location = new GeoLocation(request.Latitude, request.Longitude),
            LocationDescription = request.LocationDescription,
            ResponseRadiusKm = request.ResponseRadiusKm,
            Skills = request.Skills ?? [],
            Notes = request.Notes,
            Schedules = request.Schedules?.Select(s => new ResponderSchedule
            {
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                EffectiveFrom = s.EffectiveFrom,
                EffectiveUntil = s.EffectiveUntil
            }).ToList() ?? []
        };

        return await _repository.AddAsync(responder);
    }

    public async Task<DesignatedResponder?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<DesignatedResponderListResponse> ListAsync(int page, int pageSize, DesignatedResponderStatus? status)
    {
        var query = _repository.Query();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new DesignatedResponderListResponse(items, total, page, pageSize);
    }

    public async Task<DesignatedResponder?> UpdateStatusAsync(Guid id, UpdateDesignatedResponderStatusRequest request)
    {
        var responder = await _repository.GetByIdAsync(id);
        if (responder is null) return null;

        responder.Status = request.Status;
        responder.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(responder);
        return responder;
    }

    public async Task<List<DesignatedResponderMapItem>> GetMapItemsAsync(DesignatedResponderStatus? status)
    {
        var query = _repository.Query();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var responders = await query.ToListAsync();

        return responders.Select(r => new DesignatedResponderMapItem(
            r.Id,
            r.VolunteerName,
            r.Location.Latitude,
            r.Location.Longitude,
            r.ResponseRadiusKm,
            r.LocationDescription,
            r.Status,
            r.Skills,
            r.Schedules.Select(s => new ScheduleEntry(
                s.DayOfWeek, s.StartTime, s.EndTime, s.EffectiveFrom, s.EffectiveUntil)).ToList()
        )).ToList();
    }

    public async Task ActivateScheduledRespondersAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now);

        var approved = await _repository.Query()
            .Where(r => r.Status == DesignatedResponderStatus.Approved ||
                         r.Status == DesignatedResponderStatus.Active)
            .ToListAsync();

        foreach (var responder in approved)
        {
            var hasActiveSchedule = responder.Schedules.Any(s =>
                s.DayOfWeek == today &&
                s.StartTime <= currentTime &&
                s.EndTime >= currentTime &&
                (!s.EffectiveFrom.HasValue || s.EffectiveFrom.Value <= now) &&
                (!s.EffectiveUntil.HasValue || s.EffectiveUntil.Value >= now));

            var newStatus = hasActiveSchedule
                ? DesignatedResponderStatus.Active
                : DesignatedResponderStatus.Approved;

            if (responder.Status != newStatus)
            {
                responder.Status = newStatus;
                responder.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(responder);
            }
        }
    }
}
