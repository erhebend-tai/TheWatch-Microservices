// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

namespace TheWatch.Contracts.Surveillance.Models;

public class FootageSubmissionDto
{
    public Guid Id { get; set; }
    public Guid CameraId { get; set; }
    public Guid SubmitterId { get; set; }
    public FootageStatus Status { get; set; }
    public double GpsLatitude { get; set; }
    public double GpsLongitude { get; set; }
    public bool GpsVerified { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public double? DurationSeconds { get; set; }
    public MediaType MediaType { get; set; }
    public string? FileHashSha256 { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = [];
    public DateTime? AnalysisCompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DetectionResultDto
{
    public Guid Id { get; set; }
    public Guid FootageId { get; set; }
    public DetectionType DetectionType { get; set; }
    public float Confidence { get; set; }
    public float BoundingBoxX { get; set; }
    public float BoundingBoxY { get; set; }
    public float BoundingBoxW { get; set; }
    public float BoundingBoxH { get; set; }
    public DateTime FrameTimestamp { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? ModelVersion { get; set; }
    public string? Metadata { get; set; }
}
