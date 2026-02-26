using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Guid, Responder> _responders = new();

    public Task<Responder> RegisterAsync(RegisterResponderRequest request)
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

        _responders[responder.Id] = responder;
        return Task.FromResult(responder);
    }

    public Task<Responder?> GetByIdAsync(Guid id)
    {
        _responders.TryGetValue(id, out var responder);
        return Task.FromResult(responder);
    }

    public Task<ResponderListResponse> ListAsync(int page, int pageSize, ResponderType? type, ResponderStatus? status)
    {
        var query = _responders.Values.Where(r => r.IsActive).AsEnumerable();

        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var total = query.Count();
        var items = query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new ResponderListResponse(items, total, page, pageSize));
    }

    public Task<Responder?> UpdateLocationAsync(Guid id, UpdateLocationRequest request)
    {
        if (!_responders.TryGetValue(id, out var responder))
            return Task.FromResult<Responder?>(null);

        responder.LastKnownLocation = new GeoLocation(
            request.Latitude,
            request.Longitude,
            request.Accuracy,
            DateTime.UtcNow);
        responder.LocationUpdatedAt = DateTime.UtcNow;
        responder.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<Responder?>(responder);
    }

    public Task<Responder?> UpdateStatusAsync(Guid id, UpdateStatusRequest request)
    {
        if (!_responders.TryGetValue(id, out var responder))
            return Task.FromResult<Responder?>(null);

        responder.Status = request.Status;
        responder.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<Responder?>(responder);
    }

    public Task<List<ResponderSummary>> FindNearbyAsync(NearbyResponderQuery query)
    {
        var results = _responders.Values
            .Where(r => r.IsActive && r.LastKnownLocation is not null)
            .Where(r => !query.AvailableOnly || r.Status == ResponderStatus.Available)
            .Where(r => !query.Type.HasValue || r.Type == query.Type.Value)
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

        return Task.FromResult(results);
    }

    public Task<bool> DeactivateAsync(Guid id)
    {
        if (!_responders.TryGetValue(id, out var responder))
            return Task.FromResult(false);

        responder.IsActive = false;
        responder.Status = ResponderStatus.OffDuty;
        responder.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult(true);
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
