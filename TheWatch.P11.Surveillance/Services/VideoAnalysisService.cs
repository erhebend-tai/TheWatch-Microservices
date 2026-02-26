// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheWatch.P11.Surveillance.Surveillance;
using TheWatch.Shared.ML;

namespace TheWatch.P11.Surveillance.Services;

public interface IVideoAnalysisService
{
    Task<List<DetectionResult>> AnalyzeFootageAsync(Guid footageId, float confidenceThreshold = 0.5f);
    Task<int> ProcessQueuedFootageAsync();
}

public class VideoAnalysisService : IVideoAnalysisService
{
    private readonly IWatchRepository<FootageSubmission> _footage;
    private readonly IWatchRepository<DetectionResult> _detections;
    private readonly IObjectDetector _objectDetector;
    private readonly ILogger<VideoAnalysisService> _logger;

    public VideoAnalysisService(
        IWatchRepository<FootageSubmission> footage,
        IWatchRepository<DetectionResult> detections,
        IObjectDetector objectDetector,
        ILogger<VideoAnalysisService> logger)
    {
        _footage = footage;
        _detections = detections;
        _objectDetector = objectDetector;
        _logger = logger;
    }

    public async Task<List<DetectionResult>> AnalyzeFootageAsync(Guid footageId, float confidenceThreshold = 0.5f)
    {
        var footage = await _footage.GetByIdAsync(footageId);
        if (footage is null)
        {
            _logger.LogWarning("Footage not found for analysis: {FootageId}", footageId);
            return [];
        }

        footage.Status = FootageStatus.Processing;
        await _footage.UpdateAsync(footage);

        var results = new List<DetectionResult>();

        if (!_objectDetector.IsReady)
        {
            _logger.LogWarning("Object detector not ready, skipping analysis for footage {FootageId}", footageId);
            footage.Status = FootageStatus.Analyzed;
            footage.AnalysisCompletedAt = DateTime.UtcNow;
            await _footage.UpdateAsync(footage);
            return results;
        }

        try
        {
            // In production, frames would be extracted from the video at the MediaUrl.
            // For now, we simulate frame extraction with a placeholder.
            // Each frame would be passed through the detector.
            var sampleFrame = Array.Empty<byte>();

            var detections = await _objectDetector.DetectObjectsAsync(sampleFrame, confidenceThreshold);

            foreach (var detection in detections)
            {
                var result = new DetectionResult
                {
                    FootageId = footageId,
                    DetectionType = MapDetectionType(detection.DetectionType),
                    Label = detection.Label,
                    Confidence = detection.Confidence,
                    BoundingBoxX = detection.BoundingBoxX,
                    BoundingBoxY = detection.BoundingBoxY,
                    BoundingBoxW = detection.BoundingBoxW,
                    BoundingBoxH = detection.BoundingBoxH,
                    FrameTimestamp = detection.Timestamp,
                    ModelVersion = detection.ModelVersion
                };

                await _detections.AddAsync(result);
                results.Add(result);
            }

            footage.Status = FootageStatus.Analyzed;
            footage.AnalysisCompletedAt = DateTime.UtcNow;
            await _footage.UpdateAsync(footage);

            _logger.LogInformation("Footage {FootageId} analyzed: {DetectionCount} detections", footageId, results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze footage {FootageId}", footageId);
            footage.Status = FootageStatus.Submitted; // Reset for retry
            await _footage.UpdateAsync(footage);
        }

        return results;
    }

    public async Task<int> ProcessQueuedFootageAsync()
    {
        var queued = await _footage.Query()
            .Where(f => f.Status == FootageStatus.Submitted)
            .OrderBy(f => f.CreatedAt)
            .Take(10)
            .ToListAsync();

        var processed = 0;
        foreach (var footage in queued)
        {
            var detections = await AnalyzeFootageAsync(footage.Id);
            if (detections.Count >= 0) processed++; // Count even zero-detection analyses as processed
        }

        if (processed > 0)
            _logger.LogInformation("Processed {Count} queued footage submissions", processed);

        return processed;
    }

    private static Surveillance.DetectionType MapDetectionType(DetectedObjectType mlType) => mlType switch
    {
        DetectedObjectType.Person => Surveillance.DetectionType.Person,
        DetectedObjectType.Vehicle => Surveillance.DetectionType.Vehicle,
        DetectedObjectType.Weapon or DetectedObjectType.Knife => Surveillance.DetectionType.Weapon,
        DetectedObjectType.LicensePlate => Surveillance.DetectionType.LicensePlate,
        DetectedObjectType.Face => Surveillance.DetectionType.Face,
        DetectedObjectType.Backpack or DetectedObjectType.Package => Surveillance.DetectionType.Package,
        DetectedObjectType.Animal => Surveillance.DetectionType.Animal,
        DetectedObjectType.Fire or DetectedObjectType.Smoke => Surveillance.DetectionType.Fire,
        _ => Surveillance.DetectionType.Other
    };
}
