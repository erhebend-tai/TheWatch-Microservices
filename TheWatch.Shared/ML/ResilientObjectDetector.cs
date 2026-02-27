// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TheWatch.Shared.ML;

/// <summary>
/// Resilient object detector that chains local ONNX detection with cloud provider fallbacks.
/// Attempts detection in priority order: Local ONNX → cloud providers (Azure/GCP/AWS).
/// Used by P11 Surveillance to ensure object detection remains available even when the
/// local model is unavailable or for enhanced accuracy via cloud ML services.
/// </summary>
public class ResilientObjectDetector : IObjectDetector
{
    private readonly IObjectDetector _localDetector;
    private readonly IEnumerable<ICloudObjectDetector> _cloudDetectors;
    private readonly CloudObjectDetectorOptions _options;
    private readonly ILogger<ResilientObjectDetector> _logger;

    public ResilientObjectDetector(
        IObjectDetector localDetector,
        IEnumerable<ICloudObjectDetector> cloudDetectors,
        IOptions<CloudObjectDetectorOptions> options,
        ILogger<ResilientObjectDetector> logger)
    {
        _localDetector = localDetector;
        _cloudDetectors = cloudDetectors;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsReady => _localDetector.IsReady || (_options.EnableCloudFallback && _cloudDetectors.Any(d => d.IsConfigured));

    public async Task<IReadOnlyList<ObjectDetectionResult>> DetectObjectsAsync(
        byte[] imageData,
        float confidenceThreshold = 0.5f,
        CancellationToken cancellationToken = default)
    {
        // Try local ONNX detector first
        if (_localDetector.IsReady)
        {
            try
            {
                var results = await _localDetector.DetectObjectsAsync(imageData, confidenceThreshold, cancellationToken);
                if (results.Count > 0)
                {
                    _logger.LogDebug("Local ONNX detector returned {Count} detections", results.Count);
                    return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Local ONNX detection failed, attempting cloud fallback");
            }
        }

        // Fall back to cloud providers in priority order
        if (!_options.EnableCloudFallback)
        {
            _logger.LogDebug("Cloud fallback disabled, returning empty results");
            return [];
        }

        return await TryCloudDetectorsAsync(imageData, confidenceThreshold, cancellationToken);
    }

    public async Task<IReadOnlyList<ObjectDetectionResult>> DetectObjectsInFrameAsync(
        byte[] frameData,
        int width,
        int height,
        float confidenceThreshold = 0.5f,
        CancellationToken cancellationToken = default)
    {
        // Try local ONNX detector first (it supports frame-specific detection)
        if (_localDetector.IsReady)
        {
            try
            {
                var results = await _localDetector.DetectObjectsInFrameAsync(
                    frameData, width, height, confidenceThreshold, cancellationToken);
                if (results.Count > 0)
                {
                    _logger.LogDebug("Local ONNX frame detector returned {Count} detections", results.Count);
                    return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Local ONNX frame detection failed, attempting cloud fallback");
            }
        }

        // Cloud detectors use image-based detection as fallback
        if (!_options.EnableCloudFallback)
            return [];

        return await TryCloudDetectorsAsync(frameData, confidenceThreshold, cancellationToken);
    }

    private async Task<IReadOnlyList<ObjectDetectionResult>> TryCloudDetectorsAsync(
        byte[] imageData,
        float confidenceThreshold,
        CancellationToken cancellationToken)
    {
        var orderedProviders = _options.ProviderPriority
            .Select(name => _cloudDetectors.FirstOrDefault(d => d.ProviderName == name && d.IsConfigured))
            .Where(d => d is not null)
            .Cast<ICloudObjectDetector>();

        foreach (var detector in orderedProviders)
        {
            try
            {
                _logger.LogInformation("Attempting cloud object detection via {Provider}", detector.ProviderName);
                var results = await detector.DetectObjectsAsync(imageData, confidenceThreshold, cancellationToken);
                _logger.LogInformation("Cloud detector {Provider} returned {Count} detections",
                    detector.ProviderName, results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cloud detector {Provider} failed, trying next provider", detector.ProviderName);
            }
        }

        _logger.LogWarning("All object detection providers exhausted, returning empty results");
        return [];
    }
}
