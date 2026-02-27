// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

namespace TheWatch.Contracts.Surveillance.Models;

public record CameraLocationDto(double Latitude, double Longitude, string? Address = null, double CoverageRadiusMeters = 50, double? Heading = null);

public class CameraRegistrationDto
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public CameraLocationDto Location { get; set; } = new(0, 0);
    public CameraStatus Status { get; set; }
    public string? CameraModel { get; set; }
    public string? StreamUrl { get; set; }
    public bool IsPublic { get; set; }
    public string? Description { get; set; }
    public SubmissionSource Source { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
