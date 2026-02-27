// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

namespace TheWatch.Contracts.Surveillance.Models;

public record RegisterCameraRequest(Guid OwnerId, CameraLocationDto Location, string? CameraModel = null, string? StreamUrl = null, bool IsPublic = true, string? Description = null, SubmissionSource Source = SubmissionSource.PublicCamera);
public record SubmitFootageRequest(Guid CameraId, Guid SubmitterId, double GpsLatitude, double GpsLongitude, DateTime StartTime, DateTime EndTime, string MediaUrl, MediaType MediaType = MediaType.Video, string? FileHashSha256 = null, string? Description = null, List<string>? Tags = null);
public record ReportCrimeLocationRequest(CameraLocationDto Location, string Description, Guid ReporterId, string CrimeType, DateTime? OccurredAt = null);
public record SurveillanceSearchRequest(CameraLocationDto Location, double RadiusKm = 5.0, DateTime? TimeWindowStart = null, DateTime? TimeWindowEnd = null, List<DetectionType>? DetectionTypes = null);
