// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace TheWatch.Shared.Contracts.Mobile;

// Shared surveillance DTOs for mobile client consumption.

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

public record CameraLocationDto(
    [property: Range(-90.0, 90.0)] double Latitude,
    [property: Range(-180.0, 180.0)] double Longitude,
    [property: MaxLength(500)] string? Address = null,
    [property: Range(0, 10000)] double CoverageRadiusMeters = 50,
    [property: Range(0, 360)] double? Heading = null);

public record RegisterCameraRequest(
    [property: Required] Guid OwnerId,
    [property: Required] CameraLocationDto Location,
    [property: MaxLength(200)] string? CameraModel = null,
    [property: MaxLength(2000)] string? StreamUrl = null,
    bool IsPublic = true,
    [property: MaxLength(1000)] string? Description = null,
    SubmissionSource Source = SubmissionSource.PublicCamera);

public record CameraRegistrationDto(
    Guid Id,
    Guid OwnerId,
    CameraLocationDto Location,
    CameraStatus Status,
    string? CameraModel,
    string? StreamUrl,
    bool IsPublic,
    string? Description,
    SubmissionSource Source,
    DateTime? VerifiedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CameraListResponse(
    List<CameraRegistrationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record SubmitFootageRequest(
    [property: Required] Guid CameraId,
    [property: Required] Guid SubmitterId,
    [property: Range(-90.0, 90.0)] double GpsLatitude,
    [property: Range(-180.0, 180.0)] double GpsLongitude,
    [property: Required] DateTime StartTime,
    [property: Required] DateTime EndTime,
    [property: Required, MaxLength(2000)] string MediaUrl,
    [property: MaxLength(2000)] string? Description = null,
    List<string>? Tags = null);

public record FootageSubmissionDto(
    Guid Id,
    Guid CameraId,
    Guid SubmitterId,
    FootageStatus Status,
    double GpsLatitude,
    double GpsLongitude,
    bool GpsVerified,
    DateTime StartTime,
    DateTime EndTime,
    string MediaUrl,
    string? ThumbnailUrl,
    double? DurationSeconds,
    string? Description,
    List<string> Tags,
    List<DetectionResultDto> Detections,
    DateTime? AnalysisCompletedAt,
    DateTime CreatedAt);

public record FootageListResponse(
    List<FootageSubmissionDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record ReportCrimeLocationRequest(
    [property: Required] CameraLocationDto Location,
    [property: Required, MaxLength(2000)] string Description,
    [property: Required] Guid ReporterId,
    [property: Required, MaxLength(200)] string CrimeType,
    DateTime? OccurredAt = null);

public record CrimeLocationDto(
    Guid Id,
    CameraLocationDto Location,
    string Description,
    string CrimeType,
    Guid ReporterId,
    DateTime? OccurredAt,
    DateTime CreatedAt,
    bool IsActive,
    int NearbyFootageCount);

public record CrimeLocationListResponse(
    List<CrimeLocationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record DetectionResultDto(
    Guid Id,
    Guid FootageId,
    DetectionType DetectionType,
    float Confidence,
    float BoundingBoxX,
    float BoundingBoxY,
    float BoundingBoxW,
    float BoundingBoxH,
    DateTime FrameTimestamp,
    string Label,
    string? ModelVersion,
    string? Metadata);

public record DetectionListResponse(
    List<DetectionResultDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record SurveillanceSearchRequest(
    [property: Required] CameraLocationDto Location,
    [property: Range(0.1, 100.0)] double RadiusKm = 5.0,
    DateTime? TimeWindowStart = null,
    DateTime? TimeWindowEnd = null,
    List<DetectionType>? DetectionTypes = null);

public record SurveillanceSearchResultDto(
    FootageSubmissionDto Footage,
    List<DetectionResultDto> Detections,
    double DistanceKm);
