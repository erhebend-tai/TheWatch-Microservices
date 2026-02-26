namespace TheWatch.Contracts.DisasterRelief.Models;

public record GeoPointDto(double Latitude, double Longitude);

public class DisasterEventDto
{
    public Guid Id { get; set; }
    public DisasterType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GeoPointDto Location { get; set; } = new(0, 0);
    public double RadiusKm { get; set; }
    public int Severity { get; set; }
    public EventStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ShelterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public GeoPointDto Location { get; set; } = new(0, 0);
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public ShelterStatus Status { get; set; }
    public List<string> Amenities { get; set; } = [];
    public string? ContactPhone { get; set; }
    public Guid? DisasterEventId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ResourceItemDto
{
    public Guid Id { get; set; }
    public ResourceCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Unit { get; set; }
    public GeoPointDto Location { get; set; } = new(0, 0);
    public Guid? DonorId { get; set; }
    public ResourceStatus Status { get; set; }
    public Guid? DisasterEventId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ResourceRequestDto
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public ResourceCategory Category { get; set; }
    public int Quantity { get; set; }
    public RequestPriority Priority { get; set; }
    public GeoPointDto Location { get; set; } = new(0, 0);
    public RequestStatus Status { get; set; }
    public Guid? MatchedResourceId { get; set; }
    public Guid? DisasterEventId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EvacuationRouteDto
{
    public Guid Id { get; set; }
    public Guid DisasterEventId { get; set; }
    public GeoPointDto Origin { get; set; } = new(0, 0);
    public GeoPointDto Destination { get; set; } = new(0, 0);
    public double DistanceKm { get; set; }
    public int EstimatedTimeMinutes { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
