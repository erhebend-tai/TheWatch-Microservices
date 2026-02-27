// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

namespace TheWatch.Shared.ML;

/// <summary>
/// Cloud-based object detection provider interface.
/// Implementations wrap Azure Computer Vision, Google Cloud Vision, and AWS Rekognition.
/// Used as backup when local ONNX inference is unavailable or for enhanced accuracy.
/// </summary>
public interface ICloudObjectDetector
{
    /// <summary>
    /// Detect objects in an image using a cloud vision API.
    /// </summary>
    Task<IReadOnlyList<ObjectDetectionResult>> DetectObjectsAsync(
        byte[] imageData,
        float confidenceThreshold = 0.5f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The cloud provider name (e.g., "Azure", "GoogleCloud", "AWS").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Whether this provider is configured and available.
    /// </summary>
    bool IsConfigured { get; }
}
