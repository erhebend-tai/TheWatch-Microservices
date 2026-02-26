namespace TheWatch.P8.DisasterRelief.Relief;

public enum DisasterType
{
    Wildfire,
    Hurricane,
    Tornado,
    Flood,
    Earthquake,
    ChemicalSpill,
    Pandemic,
    Other
}

public enum EventStatus
{
    Active,
    Monitoring,
    Resolved,
    Archived
}

public enum ShelterStatus
{
    Open,
    Full,
    Closed,
    Evacuating
}

public enum ResourceCategory
{
    Water,
    Food,
    Medical,
    Clothing,
    Equipment,
    Shelter,
    Transportation,
    Other
}

public enum ResourceStatus
{
    Available,
    Reserved,
    InTransit,
    Delivered,
    Expired
}

public enum RequestPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum RequestStatus
{
    Open,
    Matched,
    Fulfilled,
    Cancelled
}

public record GeoPoint(double Latitude, double Longitude);

public class DisasterEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DisasterType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GeoPoint Location { get; set; } = new(0, 0);
    public double RadiusKm { get; set; } = 10.0;
    public int Severity { get; set; } = 3;
    public EventStatus Status { get; set; } = EventStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Shelter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public GeoPoint Location { get; set; } = new(0, 0);
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public ShelterStatus Status { get; set; } = ShelterStatus.Open;
    public List<string> Amenities { get; set; } = [];
    public string? ContactPhone { get; set; }
    public Guid? DisasterEventId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ResourceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ResourceCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Unit { get; set; }
    public GeoPoint Location { get; set; } = new(0, 0);
    public Guid? DonorId { get; set; }
    public ResourceStatus Status { get; set; } = ResourceStatus.Available;
    public Guid? DisasterEventId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ResourceRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequesterId { get; set; }
    public ResourceCategory Category { get; set; }
    public int Quantity { get; set; }
    public RequestPriority Priority { get; set; } = RequestPriority.Medium;
    public GeoPoint Location { get; set; } = new(0, 0);
    public RequestStatus Status { get; set; } = RequestStatus.Open;
    public Guid? MatchedResourceId { get; set; }
    public Guid? DisasterEventId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EvacuationRoute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DisasterEventId { get; set; }
    public GeoPoint Origin { get; set; } = new(0, 0);
    public GeoPoint Destination { get; set; } = new(0, 0);
    public double DistanceKm { get; set; }
    public int EstimatedTimeMinutes { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Request/response records

public record CreateDisasterEventRequest(
    DisasterType Type,
    string Name,
    string? Description,
    double Latitude,
    double Longitude,
    double RadiusKm = 10.0,
    int Severity = 3);

public record UpdateEventStatusRequest(EventStatus Status);

public record CreateShelterRequest(
    string Name,
    double Latitude,
    double Longitude,
    int Capacity,
    string? ContactPhone = null,
    List<string>? Amenities = null,
    Guid? DisasterEventId = null);

public record UpdateOccupancyRequest(int CurrentOccupancy);

public record DonateResourceRequest(
    ResourceCategory Category,
    string Name,
    int Quantity,
    string? Unit,
    double Latitude,
    double Longitude,
    Guid? DonorId = null,
    Guid? DisasterEventId = null);

public record CreateResourceRequestRecord(
    Guid RequesterId,
    ResourceCategory Category,
    int Quantity,
    RequestPriority Priority,
    double Latitude,
    double Longitude,
    Guid? DisasterEventId = null);

public record CreateEvacuationRouteRequest(
    Guid DisasterEventId,
    double OriginLat,
    double OriginLon,
    double DestLat,
    double DestLon,
    double DistanceKm,
    int EstimatedTimeMinutes,
    string? Description = null);

public record DisasterEventListResponse(
    List<DisasterEvent> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record ShelterListResponse(
    List<Shelter> Items,
    int TotalCount);

public record ShelterSummary(
    Guid Id,
    string Name,
    ShelterStatus Status,
    int Capacity,
    int CurrentOccupancy,
    double? DistanceKm);
