namespace TheWatch.Contracts.Geospatial.Models;

public record NearbyResultDto(Guid EntityId, string EntityType, string Label, double Longitude, double Latitude, double DistanceMeters);

public class GeoZoneDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public GeoZoneType Type { get; set; }
    public ZoneSeverity Severity { get; set; }
    public double CenterLongitude { get; set; }
    public double CenterLatitude { get; set; }
    public double RadiusMeters { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TrackedEntityDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid ExternalEntityId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double Speed { get; set; }
    public double Heading { get; set; }
    public TrackingStatus Status { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public class IncidentZoneDto
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public double CenterLongitude { get; set; }
    public double CenterLatitude { get; set; }
    public double RadiusMeters { get; set; }
    public ZoneSeverity Severity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GeofenceEventDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid GeofenceId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public DateTime Timestamp { get; set; }
}
