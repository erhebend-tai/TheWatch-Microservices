// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheWatch.P11.Surveillance.Surveillance;

namespace TheWatch.P11.Surveillance.Services;

public interface IFootageService
{
    Task<FootageSubmission> SubmitFootageAsync(SubmitFootageRequest request);
    Task<FootageSubmission?> GetFootageAsync(Guid id);
    Task<FootageListResponse> ListFootageAsync(int page = 1, int pageSize = 20, FootageStatus? statusFilter = null);
    Task<DetectionListResponse> GetDetectionsForFootageAsync(Guid footageId, int page = 1, int pageSize = 50);
    Task<FootageSubmission?> UpdateFootageStatusAsync(Guid id, FootageStatus status);
    Task<List<FootageSubmission>> FindFootageNearLocationAsync(double latitude, double longitude, double radiusKm, DateTime? start = null, DateTime? end = null);
    Task<int> ArchiveAnalyzedFootageAsync(TimeSpan olderThan);
}

public class FootageService : IFootageService
{
    private readonly IWatchRepository<FootageSubmission> _footage;
    private readonly IWatchRepository<DetectionResult> _detections;
    private readonly IWatchRepository<CameraRegistration> _cameras;
    private readonly ILogger<FootageService> _logger;

    private const double KmPerDegree = 111.0;
    private const double GpsToleranceKm = 0.5; // 500m tolerance for GPS verification

    public FootageService(
        IWatchRepository<FootageSubmission> footage,
        IWatchRepository<DetectionResult> detections,
        IWatchRepository<CameraRegistration> cameras,
        ILogger<FootageService> logger)
    {
        _footage = footage;
        _detections = detections;
        _cameras = cameras;
        _logger = logger;
    }

    public async Task<FootageSubmission> SubmitFootageAsync(SubmitFootageRequest request)
    {
        var footage = new FootageSubmission
        {
            CameraId = request.CameraId,
            SubmitterId = request.SubmitterId,
            GpsLatitude = request.GpsLatitude,
            GpsLongitude = request.GpsLongitude,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            MediaUrl = request.MediaUrl,
            MediaType = request.MediaType,
            FileHashSha256 = request.FileHashSha256,
            Description = request.Description,
            Tags = request.Tags ?? [],
            DurationSeconds = (request.EndTime - request.StartTime).TotalSeconds
        };

        // GPS verification: compare submitted GPS with registered camera location
        var camera = await _cameras.GetByIdAsync(request.CameraId);
        if (camera is not null)
        {
            var distance = HaversineDistanceKm(
                request.GpsLatitude, request.GpsLongitude,
                camera.Latitude, camera.Longitude);
            footage.GpsVerified = distance <= GpsToleranceKm;

            if (!footage.GpsVerified)
            {
                _logger.LogWarning("GPS mismatch for footage from camera {CameraId}: {Distance:F2}km from registered location",
                    request.CameraId, distance);
            }
        }

        await _footage.AddAsync(footage);
        _logger.LogInformation("Footage submitted: {FootageId} from camera {CameraId}", footage.Id, footage.CameraId);
        return footage;
    }

    public async Task<FootageSubmission?> GetFootageAsync(Guid id)
    {
        return await _footage.GetByIdAsync(id);
    }

    public async Task<FootageListResponse> ListFootageAsync(int page = 1, int pageSize = 20, FootageStatus? statusFilter = null)
    {
        var query = _footage.Query();

        if (statusFilter.HasValue)
            query = query.Where(f => f.Status == statusFilter.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new FootageListResponse(items, totalCount, page, pageSize);
    }

    public async Task<DetectionListResponse> GetDetectionsForFootageAsync(Guid footageId, int page = 1, int pageSize = 50)
    {
        var query = _detections.Query().Where(d => d.FootageId == footageId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.Confidence)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new DetectionListResponse(items, totalCount, page, pageSize);
    }

    public async Task<FootageSubmission?> UpdateFootageStatusAsync(Guid id, FootageStatus status)
    {
        var footage = await _footage.GetByIdAsync(id);
        if (footage is null) return null;

        footage.Status = status;
        if (status == FootageStatus.Analyzed)
            footage.AnalysisCompletedAt = DateTime.UtcNow;

        await _footage.UpdateAsync(footage);
        return footage;
    }

    public async Task<List<FootageSubmission>> FindFootageNearLocationAsync(
        double latitude, double longitude, double radiusKm,
        DateTime? start = null, DateTime? end = null)
    {
        var latDelta = radiusKm / KmPerDegree;
        var lonDelta = radiusKm / (KmPerDegree * Math.Cos(latitude * Math.PI / 180));

        var query = _footage.Query()
            .Where(f => f.GpsLatitude >= latitude - latDelta && f.GpsLatitude <= latitude + latDelta)
            .Where(f => f.GpsLongitude >= longitude - lonDelta && f.GpsLongitude <= longitude + lonDelta);

        if (start.HasValue)
            query = query.Where(f => f.EndTime >= start.Value);
        if (end.HasValue)
            query = query.Where(f => f.StartTime <= end.Value);

        return await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
    }

    public async Task<int> ArchiveAnalyzedFootageAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var toArchive = await _footage.Query()
            .Where(f => f.Status == FootageStatus.Analyzed && f.AnalysisCompletedAt < cutoff)
            .ToListAsync();

        foreach (var footage in toArchive)
        {
            footage.Status = FootageStatus.Archived;
            await _footage.UpdateAsync(footage);
        }

        return toArchive.Count;
    }

    private static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0; // Earth radius in km
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
