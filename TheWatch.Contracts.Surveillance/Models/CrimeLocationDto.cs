// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

namespace TheWatch.Contracts.Surveillance.Models;

public class CrimeLocationDto
{
    public Guid Id { get; set; }
    public CameraLocationDto Location { get; set; } = new(0, 0);
    public string Description { get; set; } = string.Empty;
    public string CrimeType { get; set; } = string.Empty;
    public Guid ReporterId { get; set; }
    public DateTime? OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int NearbyFootageCount { get; set; }
}

public class SurveillanceSearchResultDto
{
    public FootageSubmissionDto Footage { get; set; } = null!;
    public List<DetectionResultDto> Detections { get; set; } = [];
    public double DistanceKm { get; set; }
}

public class SurveillanceStatsDto
{
    public int TotalCameras { get; set; }
    public int VerifiedCameras { get; set; }
    public int TotalFootageSubmissions { get; set; }
    public int AnalyzedFootage { get; set; }
    public int TotalDetections { get; set; }
    public int ActiveCrimeLocations { get; set; }
}
