using Microsoft.EntityFrameworkCore;
using TheWatch.P6.FirstResponder.Responders;

namespace TheWatch.P6.FirstResponder.Services;

public interface ICheckInService
{
    Task<CheckIn> CreateAsync(Guid responderId, CreateCheckInRequest request);
    Task<List<CheckIn>> GetForIncidentAsync(Guid incidentId);
    Task<List<CheckIn>> GetForResponderAsync(Guid responderId);
    Task CleanupOldCheckInsAsync(TimeSpan olderThan);
}

public class CheckInService : ICheckInService
{
    private readonly IWatchRepository<CheckIn> _checkIns;
    private readonly IResponderService _responderService;

    public CheckInService(IWatchRepository<CheckIn> checkIns, IResponderService responderService)
    {
        _checkIns = checkIns;
        _responderService = responderService;
    }

    public async Task<CheckIn> CreateAsync(Guid responderId, CreateCheckInRequest request)
    {
        var checkIn = new CheckIn
        {
            ResponderId = responderId,
            IncidentId = request.IncidentId,
            Type = request.Type,
            Location = new GeoLocation(request.Latitude, request.Longitude),
            Notes = request.Notes
        };

        await _checkIns.AddAsync(checkIn);

        // Update responder location as side-effect
        await _responderService.UpdateLocationAsync(responderId, new UpdateLocationRequest(
            request.Latitude, request.Longitude));

        // Update responder status based on check-in type
        var newStatus = request.Type switch
        {
            CheckInType.Arrived => ResponderStatus.OnScene,
            CheckInType.Departing => ResponderStatus.Available,
            CheckInType.AllClear => ResponderStatus.Available,
            _ => (ResponderStatus?)null
        };

        if (newStatus.HasValue)
        {
            await _responderService.UpdateStatusAsync(responderId, new UpdateStatusRequest(newStatus.Value));
        }

        return checkIn;
    }

    public async Task<List<CheckIn>> GetForIncidentAsync(Guid incidentId)
    {
        return await _checkIns.Query()
            .Where(c => c.IncidentId == incidentId)
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync();
    }

    public async Task<List<CheckIn>> GetForResponderAsync(Guid responderId)
    {
        return await _checkIns.Query()
            .Where(c => c.ResponderId == responderId)
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync();
    }

    public async Task CleanupOldCheckInsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var old = await _checkIns.Query()
            .Where(c => c.Timestamp < cutoff)
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var id in old)
            await _checkIns.DeleteAsync(id);
    }
}
