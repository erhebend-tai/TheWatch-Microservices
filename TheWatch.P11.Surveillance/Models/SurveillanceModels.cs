// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

namespace TheWatch.P11.Surveillance.Surveillance;

public enum CameraStatus
{
    Pending,
    Verified,
    Active,
    Inactive,
    Flagged,
    Rejected
}

public enum FootageStatus
{
    Submitted,
    Processing,
    Analyzed,
    Verified,
    Rejected,
    Archived
}

public enum DetectionType
{
    Person,
    Vehicle,
    Weapon,
    LicensePlate,
    Face,
    Package,
    Animal,
    Fire,
    Other
}

public enum SubmissionSource
{
    PublicCamera,
    PrivateCamera,
    Doorbell,
    Dashcam,
    Bodycam,
    Drone,
    Other
}

public enum MediaType
{
    Video,
    Audio,
    Image
}

public class CameraRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public double CoverageRadiusMeters { get; set; } = 50;
    public double? Heading { get; set; }
    public string? CameraModel { get; set; }
    public string? StreamUrl { get; set; }
    public bool IsPublic { get; set; } = true;
    public CameraStatus Status { get; set; } = CameraStatus.Pending;
    public SubmissionSource Source { get; set; } = SubmissionSource.PublicCamera;
    public string? Description { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class FootageSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CameraId { get; set; }
    public Guid SubmitterId { get; set; }
    public double GpsLatitude { get; set; }
    public double GpsLongitude { get; set; }
    public bool GpsVerified { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public double? DurationSeconds { get; set; }
    public FootageStatus Status { get; set; } = FootageStatus.Submitted;
    public MediaType MediaType { get; set; } = MediaType.Video;
    public string? FileHashSha256 { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = [];
    public DateTime? AnalysisCompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CrimeLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CrimeType { get; set; } = string.Empty;
    public Guid ReporterId { get; set; }
    public DateTime? OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class DetectionResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FootageId { get; set; }
    public DetectionType DetectionType { get; set; }
    public string Label { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public float BoundingBoxX { get; set; }
    public float BoundingBoxY { get; set; }
    public float BoundingBoxW { get; set; }
    public float BoundingBoxH { get; set; }
    public DateTime FrameTimestamp { get; set; }
    public string? ModelVersion { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Request/response DTOs

public record RegisterCameraRequest(
    Guid OwnerId,
    double Latitude,
    double Longitude,
    string? Address = null,
    double CoverageRadiusMeters = 50,
    double? Heading = null,
    string? CameraModel = null,
    string? StreamUrl = null,
    bool IsPublic = true,
    string? Description = null,
    SubmissionSource Source = SubmissionSource.PublicCamera);

public record SubmitFootageRequest(
    Guid CameraId,
    Guid SubmitterId,
    double GpsLatitude,
    double GpsLongitude,
    DateTime StartTime,
    DateTime EndTime,
    string MediaUrl,
    MediaType MediaType = MediaType.Video,
    string? FileHashSha256 = null,
    string? Description = null,
    List<string>? Tags = null);

public record ReportCrimeLocationRequest(
    double Latitude,
    double Longitude,
    string Description,
    Guid ReporterId,
    string CrimeType,
    DateTime? OccurredAt = null);

public record SurveillanceSearchRequest(
    double Latitude,
    double Longitude,
    double RadiusKm = 5.0,
    DateTime? TimeWindowStart = null,
    DateTime? TimeWindowEnd = null,
    List<DetectionType>? DetectionTypes = null);

public record CameraListResponse(
    List<CameraRegistration> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record FootageListResponse(
    List<FootageSubmission> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record CrimeLocationListResponse(
    List<CrimeLocation> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record DetectionListResponse(
    List<DetectionResult> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record SurveillanceSearchResult(
    FootageSubmission Footage,
    List<DetectionResult> Detections,
    double DistanceKm);

public record SurveillanceStats(
    int TotalCameras,
    int VerifiedCameras,
    int TotalFootageSubmissions,
    int AnalyzedFootage,
    int TotalDetections,
    int ActiveCrimeLocations);
