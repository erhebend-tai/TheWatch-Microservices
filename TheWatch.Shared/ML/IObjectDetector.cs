// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

namespace TheWatch.Shared.ML;

/// <summary>
/// Object detection result from ML inference on a video frame or image.
/// </summary>
public record ObjectDetectionResult
{
    public string Label { get; init; } = string.Empty;
    public DetectedObjectType DetectionType { get; init; }
    public float Confidence { get; init; }
    public float BoundingBoxX { get; init; }
    public float BoundingBoxY { get; init; }
    public float BoundingBoxW { get; init; }
    public float BoundingBoxH { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string ModelVersion { get; init; } = "1.0.0";
}

public enum DetectedObjectType
{
    Person,
    Vehicle,
    Weapon,
    Knife,
    LicensePlate,
    Backpack,
    Face,
    Fire,
    Smoke,
    Animal,
    Package,
    Other
}

/// <summary>
/// Object detector interface for ONNX-based visual detection.
/// Used by P11 Surveillance for CCTV frame analysis.
/// </summary>
public interface IObjectDetector
{
    /// <summary>
    /// Detect objects in an image. Returns all detections above the confidence threshold.
    /// </summary>
    Task<IReadOnlyList<ObjectDetectionResult>> DetectObjectsAsync(
        byte[] imageData,
        float confidenceThreshold = 0.5f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect objects in a raw video frame with known dimensions.
    /// </summary>
    Task<IReadOnlyList<ObjectDetectionResult>> DetectObjectsInFrameAsync(
        byte[] frameData,
        int width,
        int height,
        float confidenceThreshold = 0.5f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the detector model is loaded and ready.
    /// </summary>
    bool IsReady { get; }
}

/// <summary>
/// Well-known object detection labels for surveillance analysis.
/// </summary>
public static class ObjectDetectionLabels
{
    public const string Person = "person";
    public const string Vehicle = "vehicle";
    public const string Car = "car";
    public const string Truck = "truck";
    public const string Weapon = "weapon";
    public const string Knife = "knife";
    public const string LicensePlate = "license_plate";
    public const string Backpack = "backpack";
    public const string Face = "face";
    public const string Fire = "fire";
    public const string Smoke = "smoke";

    public static readonly IReadOnlySet<string> SuspiciousLabels = new HashSet<string>
    {
        Weapon, Knife, Fire, Smoke
    };

    public static DetectedObjectType ToDetectedObjectType(string label) => label switch
    {
        Person => DetectedObjectType.Person,
        Vehicle or Car or Truck => DetectedObjectType.Vehicle,
        Weapon => DetectedObjectType.Weapon,
        Knife => DetectedObjectType.Knife,
        LicensePlate => DetectedObjectType.LicensePlate,
        Backpack => DetectedObjectType.Backpack,
        Face => DetectedObjectType.Face,
        Fire => DetectedObjectType.Fire,
        Smoke => DetectedObjectType.Smoke,
        _ => DetectedObjectType.Other
    };
}
