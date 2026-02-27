using Microsoft.EntityFrameworkCore;
using TheWatch.P9.DoctorServices.Doctors;

namespace TheWatch.P9.DoctorServices.Services;

public interface IDoctorService
{
    Task<DoctorProfile> CreateProfileAsync(CreateDoctorProfileRequest request);
    Task<DoctorProfile?> GetByIdAsync(Guid id);
    Task<DoctorListResponse> ListAsync(int page = 1, int pageSize = 20);
    Task<List<DoctorSummary>> SearchAsync(DoctorSearchQuery query);
}

public class DoctorService : IDoctorService
{
    private readonly IWatchRepository<DoctorProfile> _doctors;

    public DoctorService(IWatchRepository<DoctorProfile> doctors)
    {
        _doctors = doctors;
    }

    public async Task<DoctorProfile> CreateProfileAsync(CreateDoctorProfileRequest request)
    {
        var doctor = new DoctorProfile
        {
            Name = request.Name,
            Specializations = request.Specializations,
            LicenseNumber = request.LicenseNumber,
            Phone = request.Phone,
            Email = request.Email,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        return await _doctors.AddAsync(doctor);
    }

    public async Task<DoctorProfile?> GetByIdAsync(Guid id)
    {
        return await _doctors.GetByIdAsync(id);
    }

    public async Task<DoctorListResponse> ListAsync(int page, int pageSize)
    {
        var total = await _doctors.Query().CountAsync();
        var items = await _doctors.Query()
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new DoctorListResponse(items, total, page, pageSize);
    }

    public async Task<List<DoctorSummary>> SearchAsync(DoctorSearchQuery query)
    {
        // Load candidates from DB, do Haversine + complex filtering in-memory
        var dbQuery = _doctors.Query();

        if (query.AcceptingOnly == true)
            dbQuery = dbQuery.Where(d => d.AcceptingPatients);

        var candidates = await dbQuery.ToListAsync();

        IEnumerable<DoctorProfile> results = candidates;

        if (!string.IsNullOrWhiteSpace(query.Specialization))
            results = results.Where(d => d.Specializations.Any(s =>
                s.Contains(query.Specialization, StringComparison.OrdinalIgnoreCase)));

        var summaries = results.Select(d =>
        {
            double? dist = null;
            if (query.Latitude.HasValue && query.Longitude.HasValue && d.Latitude.HasValue && d.Longitude.HasValue)
                dist = HaversineKm(query.Latitude.Value, query.Longitude.Value, d.Latitude.Value, d.Longitude.Value);

            return new DoctorSummary(d.Id, d.Name, d.Specializations, d.Rating, dist.HasValue ? Math.Round(dist.Value, 2) : null);
        });

        if (query.RadiusKm.HasValue && query.Latitude.HasValue)
            summaries = summaries.Where(s => s.DistanceKm.HasValue && s.DistanceKm <= query.RadiusKm.Value);

        return summaries.OrderBy(s => s.DistanceKm ?? double.MaxValue).ToList();
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
