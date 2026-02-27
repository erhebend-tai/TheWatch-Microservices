// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheWatch.P11.Surveillance.Surveillance;

namespace TheWatch.P11.Surveillance.Services;

public interface ICrimeLocationService
{
    Task<CrimeLocation> ReportCrimeLocationAsync(ReportCrimeLocationRequest request);
    Task<CrimeLocation?> GetCrimeLocationAsync(Guid id);
    Task<CrimeLocationListResponse> ListCrimeLocationsAsync(int page = 1, int pageSize = 20, bool? activeOnly = true);
    Task<List<FootageSubmission>> GetFootageNearCrimeLocationAsync(Guid crimeLocationId, double radiusKm = 2.0);
    Task<int> GetNearbyFootageCountAsync(double latitude, double longitude, double radiusKm = 2.0);
}

public class CrimeLocationService : ICrimeLocationService
{
    private readonly IWatchRepository<CrimeLocation> _crimeLocations;
    private readonly IFootageService _footageService;
    private readonly ILogger<CrimeLocationService> _logger;

    public CrimeLocationService(
        IWatchRepository<CrimeLocation> crimeLocations,
        IFootageService footageService,
        ILogger<CrimeLocationService> logger)
    {
        _crimeLocations = crimeLocations;
        _footageService = footageService;
        _logger = logger;
    }

    public async Task<CrimeLocation> ReportCrimeLocationAsync(ReportCrimeLocationRequest request)
    {
        var crimeLocation = new CrimeLocation
        {
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Description = request.Description,
            CrimeType = request.CrimeType,
            ReporterId = request.ReporterId,
            OccurredAt = request.OccurredAt ?? DateTime.UtcNow
        };

        await _crimeLocations.AddAsync(crimeLocation);
        _logger.LogInformation("Crime location reported: {CrimeLocationId} type={CrimeType}", crimeLocation.Id, crimeLocation.CrimeType);
        return crimeLocation;
    }

    public async Task<CrimeLocation?> GetCrimeLocationAsync(Guid id)
    {
        return await _crimeLocations.GetByIdAsync(id);
    }

    public async Task<CrimeLocationListResponse> ListCrimeLocationsAsync(int page = 1, int pageSize = 20, bool? activeOnly = true)
    {
        var query = _crimeLocations.Query();

        if (activeOnly == true)
            query = query.Where(c => c.IsActive);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new CrimeLocationListResponse(items, totalCount, page, pageSize);
    }

    public async Task<List<FootageSubmission>> GetFootageNearCrimeLocationAsync(Guid crimeLocationId, double radiusKm = 2.0)
    {
        var crimeLocation = await _crimeLocations.GetByIdAsync(crimeLocationId);
        if (crimeLocation is null) return [];

        // Search footage within radius and time window around the crime
        var timeWindowStart = crimeLocation.OccurredAt?.AddHours(-2);
        var timeWindowEnd = crimeLocation.OccurredAt?.AddHours(2);

        return await _footageService.FindFootageNearLocationAsync(
            crimeLocation.Latitude, crimeLocation.Longitude, radiusKm,
            timeWindowStart, timeWindowEnd);
    }

    public async Task<int> GetNearbyFootageCountAsync(double latitude, double longitude, double radiusKm = 2.0)
    {
        var footage = await _footageService.FindFootageNearLocationAsync(latitude, longitude, radiusKm);
        return footage.Count;
    }
}
