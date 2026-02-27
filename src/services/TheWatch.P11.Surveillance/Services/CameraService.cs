// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheWatch.P11.Surveillance.Surveillance;

namespace TheWatch.P11.Surveillance.Services;

public interface ICameraService
{
    Task<CameraRegistration> RegisterCameraAsync(RegisterCameraRequest request);
    Task<CameraRegistration?> GetCameraAsync(Guid id);
    Task<CameraListResponse> ListCamerasAsync(int page = 1, int pageSize = 20, CameraStatus? statusFilter = null);
    Task<CameraRegistration?> VerifyCameraAsync(Guid id);
    Task<bool> DeactivateCameraAsync(Guid id);
    Task<List<CameraRegistration>> FindNearbyAsync(double latitude, double longitude, double radiusKm);
    Task<int> HealthCheckCamerasAsync();
}

public class CameraService : ICameraService
{
    private readonly IWatchRepository<CameraRegistration> _cameras;
    private readonly ILogger<CameraService> _logger;

    // ~111 km per degree of latitude
    private const double KmPerDegree = 111.0;

    public CameraService(IWatchRepository<CameraRegistration> cameras, ILogger<CameraService> logger)
    {
        _cameras = cameras;
        _logger = logger;
    }

    public async Task<CameraRegistration> RegisterCameraAsync(RegisterCameraRequest request)
    {
        var camera = new CameraRegistration
        {
            OwnerId = request.OwnerId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            CoverageRadiusMeters = request.CoverageRadiusMeters,
            Heading = request.Heading,
            CameraModel = request.CameraModel,
            StreamUrl = request.StreamUrl,
            IsPublic = request.IsPublic,
            Description = request.Description,
            Source = request.Source
        };

        await _cameras.AddAsync(camera);
        _logger.LogInformation("Camera registered: {CameraId} by owner {OwnerId}", camera.Id, camera.OwnerId);
        return camera;
    }

    public async Task<CameraRegistration?> GetCameraAsync(Guid id)
    {
        return await _cameras.GetByIdAsync(id);
    }

    public async Task<CameraListResponse> ListCamerasAsync(int page = 1, int pageSize = 20, CameraStatus? statusFilter = null)
    {
        var query = _cameras.Query();

        if (statusFilter.HasValue)
            query = query.Where(c => c.Status == statusFilter.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new CameraListResponse(items, totalCount, page, pageSize);
    }

    public async Task<CameraRegistration?> VerifyCameraAsync(Guid id)
    {
        var camera = await _cameras.GetByIdAsync(id);
        if (camera is null) return null;

        camera.Status = CameraStatus.Verified;
        camera.VerifiedAt = DateTime.UtcNow;
        camera.UpdatedAt = DateTime.UtcNow;
        await _cameras.UpdateAsync(camera);

        _logger.LogInformation("Camera verified: {CameraId}", id);
        return camera;
    }

    public async Task<bool> DeactivateCameraAsync(Guid id)
    {
        var camera = await _cameras.GetByIdAsync(id);
        if (camera is null) return false;

        camera.Status = CameraStatus.Inactive;
        camera.UpdatedAt = DateTime.UtcNow;
        await _cameras.UpdateAsync(camera);

        _logger.LogInformation("Camera deactivated: {CameraId}", id);
        return true;
    }

    public async Task<List<CameraRegistration>> FindNearbyAsync(double latitude, double longitude, double radiusKm)
    {
        var latDelta = radiusKm / KmPerDegree;
        var lonDelta = radiusKm / (KmPerDegree * Math.Cos(latitude * Math.PI / 180));

        return await _cameras.Query()
            .Where(c => c.Status == CameraStatus.Verified || c.Status == CameraStatus.Active)
            .Where(c => c.Latitude >= latitude - latDelta && c.Latitude <= latitude + latDelta)
            .Where(c => c.Longitude >= longitude - lonDelta && c.Longitude <= longitude + lonDelta)
            .ToListAsync();
    }

    public async Task<int> HealthCheckCamerasAsync()
    {
        // Mark cameras that haven't been updated in 24h as inactive
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var staleCameras = await _cameras.Query()
            .Where(c => c.Status == CameraStatus.Active && c.UpdatedAt < cutoff)
            .ToListAsync();

        foreach (var camera in staleCameras)
        {
            camera.Status = CameraStatus.Inactive;
            camera.UpdatedAt = DateTime.UtcNow;
            await _cameras.UpdateAsync(camera);
        }

        if (staleCameras.Count > 0)
            _logger.LogInformation("Camera health check: {Count} cameras marked inactive", staleCameras.Count);

        return staleCameras.Count;
    }
}
