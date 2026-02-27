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

// ─── Object Tracking (multi-cloud ML backup) ───

public enum TrackingStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// A tracking session that correlates object detections across multiple footage submissions
/// radiating outward from a crime location.
/// </summary>
public class TrackingSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CrimeLocationId { get; set; }
    public Guid InitiatedBy { get; set; }
    public string ObjectDescription { get; set; } = string.Empty;
    public List<DetectionType> TargetObjectTypes { get; set; } = [];
    public double SearchRadiusKm { get; set; } = 5.0;
    public DateTime? TimeWindowStart { get; set; }
    public DateTime? TimeWindowEnd { get; set; }
    public TrackingStatus Status { get; set; } = TrackingStatus.Pending;
    public int FootageAnalyzedCount { get; set; }
    public int MatchesFoundCount { get; set; }
    public string? DetectionProvider { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// A match found during object tracking — links a detection in footage
/// to the tracking session with distance from the crime scene.
/// </summary>
public class TrackedObjectMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TrackingSessionId { get; set; }
    public Guid FootageId { get; set; }
    public Guid DetectionId { get; set; }
    public double DistanceFromSceneKm { get; set; }
    public float Confidence { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ─── Object Tracking DTOs ───

public record ObjectTrackingRequest(
    Guid CrimeLocationId,
    Guid InitiatedBy,
    string ObjectDescription,
    List<DetectionType>? TargetObjectTypes = null,
    double SearchRadiusKm = 5.0,
    DateTime? TimeWindowStart = null,
    DateTime? TimeWindowEnd = null,
    string? FootageMediaUrl = null);

public record ObjectTrackingResponse(
    Guid TrackingSessionId,
    TrackingStatus Status,
    int FootageAnalyzedCount,
    int MatchesFoundCount,
    string? DetectionProvider,
    List<TrackedObjectMatch> Matches);

public record TrackingSessionListResponse(
    List<TrackingSession> Items,
    int TotalCount,
    int Page,
    int PageSize);

// ─── Alibi Verification ───

public enum AlibiVerdict
{
    /// <summary>All alibi checkpoints have corroborating footage evidence.</summary>
    Supported,
    /// <summary>Partial or no footage evidence found at claimed locations.</summary>
    Inconclusive,
    /// <summary>Reserved: evidence found at the crime scene during the crime window contradicts the alibi.
    /// Requires cross-referencing with a crime location; not yet implemented.</summary>
    Contradicted
}

/// <summary>
/// A single claimed location and time window provided as part of an alibi.
/// </summary>
public record AlibiCheckpoint(
    double Latitude,
    double Longitude,
    DateTime WindowStart,
    DateTime WindowEnd,
    string Description,
    double SearchRadiusKm = 0.5);

/// <summary>
/// A single detection that supports an alibi checkpoint, returned as part of verification results.
/// </summary>
public record AlibiEvidenceItem(
    Guid FootageId,
    Guid DetectionId,
    string Label,
    float Confidence,
    double DistanceFromCheckpointKm,
    DateTime DetectedAt);

/// <summary>
/// The verification result for a single alibi checkpoint.
/// </summary>
public class AlibiCheckpointResult
{
    public AlibiCheckpoint Checkpoint { get; set; } = null!;
    public bool HasEvidence { get; set; }
    public int FootageChecked { get; set; }
    public List<AlibiEvidenceItem> SupportingEvidence { get; set; } = [];
}

/// <summary>
/// Persisted record of an alibi verification request and its outcome.
/// </summary>
public class AlibiVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubjectId { get; set; }
    public string SubjectDescription { get; set; } = string.Empty;
    public Guid InitiatedBy { get; set; }
    public AlibiVerdict Verdict { get; set; } = AlibiVerdict.Inconclusive;
    public int CheckpointsTotal { get; set; }
    public int CheckpointsSupported { get; set; }
    public int TotalFootageChecked { get; set; }
    public int TotalEvidenceFound { get; set; }
    public string? DetectionProvider { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public record AlibiVerificationRequest(
    Guid SubjectId,
    string SubjectDescription,
    List<AlibiCheckpoint> Checkpoints,
    Guid InitiatedBy,
    List<DetectionType>? TargetObjectTypes = null);

public record AlibiVerificationResponse(
    Guid VerificationId,
    AlibiVerdict Verdict,
    int CheckpointsTotal,
    int CheckpointsSupported,
    int TotalFootageChecked,
    int TotalEvidenceFound,
    string? DetectionProvider,
    List<AlibiCheckpointResult> CheckpointResults);
