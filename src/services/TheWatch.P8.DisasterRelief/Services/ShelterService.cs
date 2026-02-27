using Microsoft.EntityFrameworkCore;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Services;

public interface IShelterService
{
    Task<Shelter> CreateAsync(CreateShelterRequest request);
    Task<Shelter?> GetByIdAsync(Guid id);
    Task<ShelterListResponse> ListAsync(Guid? disasterEventId = null, ShelterStatus? status = null);
    Task<List<ShelterSummary>> FindNearbyAsync(double lat, double lon, double radiusKm = 50.0);
    Task<Shelter?> UpdateOccupancyAsync(Guid id, UpdateOccupancyRequest request);
    Task<List<Shelter>> GetOverCapacitySheltersAsync(double threshold = 0.9);
}

public class ShelterService : IShelterService
{
    private readonly IWatchRepository<Shelter> _shelters;

    public ShelterService(IWatchRepository<Shelter> shelters)
    {
        _shelters = shelters;
    }

    public async Task<Shelter> CreateAsync(CreateShelterRequest request)
    {
        var shelter = new Shelter
        {
            Name = request.Name,
            Location = new GeoPoint(request.Latitude, request.Longitude),
            Capacity = request.Capacity,
            ContactPhone = request.ContactPhone,
            Amenities = request.Amenities ?? [],
            DisasterEventId = request.DisasterEventId
        };

        return await _shelters.AddAsync(shelter);
    }

    public async Task<Shelter?> GetByIdAsync(Guid id)
    {
        return await _shelters.GetByIdAsync(id);
    }

    public async Task<ShelterListResponse> ListAsync(Guid? disasterEventId, ShelterStatus? status)
    {
        var query = _shelters.Query();

        if (disasterEventId.HasValue)
            query = query.Where(s => s.DisasterEventId == disasterEventId.Value);
        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var items = await query.OrderBy(s => s.Name).ToListAsync();
        return new ShelterListResponse(items, items.Count);
    }

    public async Task<List<ShelterSummary>> FindNearbyAsync(double lat, double lon, double radiusKm)
    {
        // Load candidates, then compute Haversine in-memory
        var candidates = await _shelters.Query()
            .Where(s => s.Status != ShelterStatus.Closed)
            .ToListAsync();

        return candidates
            .Select(s =>
            {
                var dist = HaversineKm(lat, lon, s.Location.Latitude, s.Location.Longitude);
                return (Shelter: s, Distance: dist);
            })
            .Where(x => x.Distance <= radiusKm)
            .OrderBy(x => x.Distance)
            .Select(x => new ShelterSummary(
                x.Shelter.Id,
                x.Shelter.Name,
                x.Shelter.Status,
                x.Shelter.Capacity,
                x.Shelter.CurrentOccupancy,
                Math.Round(x.Distance, 2)))
            .ToList();
    }

    public async Task<Shelter?> UpdateOccupancyAsync(Guid id, UpdateOccupancyRequest request)
    {
        var shelter = await _shelters.GetByIdAsync(id);
        if (shelter is null) return null;

        shelter.CurrentOccupancy = request.CurrentOccupancy;
        shelter.UpdatedAt = DateTime.UtcNow;

        // Auto-set status based on occupancy
        if (shelter.CurrentOccupancy >= shelter.Capacity)
            shelter.Status = ShelterStatus.Full;
        else if (shelter.Status == ShelterStatus.Full)
            shelter.Status = ShelterStatus.Open;

        await _shelters.UpdateAsync(shelter);
        return shelter;
    }

    public async Task<List<Shelter>> GetOverCapacitySheltersAsync(double threshold)
    {
        var shelters = await _shelters.Query()
            .Where(s => s.Status == ShelterStatus.Open && s.Capacity > 0)
            .ToListAsync();

        return shelters
            .Where(s => (double)s.CurrentOccupancy / s.Capacity >= threshold)
            .ToList();
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
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
