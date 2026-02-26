using Microsoft.EntityFrameworkCore;
using TheWatch.P6.FirstResponder.Responders;

namespace TheWatch.P6.FirstResponder.Services;

public interface IResponderService
{
    Task<Responder> RegisterAsync(RegisterResponderRequest request);
    Task<Responder?> GetByIdAsync(Guid id);
    Task<ResponderListResponse> ListAsync(int page = 1, int pageSize = 20, ResponderType? type = null, ResponderStatus? status = null);
    Task<Responder?> UpdateLocationAsync(Guid id, UpdateLocationRequest request);
    Task<Responder?> UpdateStatusAsync(Guid id, UpdateStatusRequest request);
    Task<List<ResponderSummary>> FindNearbyAsync(NearbyResponderQuery query);
    Task<bool> DeactivateAsync(Guid id);
}

public class ResponderService : IResponderService
{
    private readonly IWatchRepository<Responder> _responders;

    public ResponderService(IWatchRepository<Responder> responders)
    {
        _responders = responders;
    }

    public async Task<Responder> RegisterAsync(RegisterResponderRequest request)
    {
        var responder = new Responder
        {
            Name = request.Name,
            Email = request.Email,
            Type = request.Type,
            BadgeNumber = request.BadgeNumber,
            Phone = request.Phone,
            Certifications = request.Certifications ?? [],
            MaxResponseRadiusKm = request.MaxResponseRadiusKm
        };

        return await _responders.AddAsync(responder);
    }

    public async Task<Responder?> GetByIdAsync(Guid id)
    {
        return await _responders.GetByIdAsync(id);
    }

    public async Task<ResponderListResponse> ListAsync(int page, int pageSize, ResponderType? type, ResponderStatus? status)
    {
        var query = _responders.Query().Where(r => r.IsActive);

        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ResponderListResponse(items, total, page, pageSize);
    }

    public async Task<Responder?> UpdateLocationAsync(Guid id, UpdateLocationRequest request)
    {
        var responder = await _responders.GetByIdAsync(id);
        if (responder is null) return null;

        responder.LastKnownLocation = new GeoLocation(
            request.Latitude,
            request.Longitude,
            request.Accuracy,
            DateTime.UtcNow);
        responder.LocationUpdatedAt = DateTime.UtcNow;
        responder.UpdatedAt = DateTime.UtcNow;

        await _responders.UpdateAsync(responder);
        return responder;
    }

    public async Task<Responder?> UpdateStatusAsync(Guid id, UpdateStatusRequest request)
    {
        var responder = await _responders.GetByIdAsync(id);
        if (responder is null) return null;

        responder.Status = request.Status;
        responder.UpdatedAt = DateTime.UtcNow;

        await _responders.UpdateAsync(responder);
        return responder;
    }

    public async Task<List<ResponderSummary>> FindNearbyAsync(NearbyResponderQuery query)
    {
        // Load candidates from DB, then do Haversine in-memory
        var candidates = await _responders.Query()
            .Where(r => r.IsActive)
            .Where(r => !query.AvailableOnly || r.Status == ResponderStatus.Available)
            .Where(r => !query.Type.HasValue || r.Type == query.Type.Value)
            .ToListAsync();

        return candidates
            .Where(r => r.LastKnownLocation is not null)
            .Select(r =>
            {
                var dist = HaversineKm(
                    query.Latitude, query.Longitude,
                    r.LastKnownLocation!.Latitude, r.LastKnownLocation.Longitude);
                return (Responder: r, Distance: dist);
            })
            .Where(x => x.Distance <= query.RadiusKm)
            .OrderBy(x => x.Distance)
            .Select(x => new ResponderSummary(
                x.Responder.Id,
                x.Responder.Name,
                x.Responder.Type,
                x.Responder.Status,
                Math.Round(x.Distance, 2),
                x.Responder.LastKnownLocation))
            .ToList();
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var responder = await _responders.GetByIdAsync(id);
        if (responder is null) return false;

        responder.IsActive = false;
        responder.Status = ResponderStatus.OffDuty;
        responder.UpdatedAt = DateTime.UtcNow;
        await _responders.UpdateAsync(responder);
        return true;
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0; // Earth radius in km
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
