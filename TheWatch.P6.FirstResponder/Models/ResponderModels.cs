namespace TheWatch.P6.FirstResponder.Responders;

public enum ResponderType
{
    Police,
    Fire,
    EMS,
    SAR,          // Search and Rescue
    HazMat,
    VolunteerMedic,
    CommunityWatch,
    Other
}

public enum ResponderStatus
{
    Available,
    Busy,
    EnRoute,
    OnScene,
    OffDuty
}

public enum CheckInType
{
    Arrived,
    Update,
    NeedBackup,
    AllClear,
    Departing
}

public record GeoLocation(
    double Latitude,
    double Longitude,
    double? Accuracy = null,
    DateTime? Timestamp = null);

public class Responder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? BadgeNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public ResponderType Type { get; set; }
    public ResponderStatus Status { get; set; } = ResponderStatus.OffDuty;
    public GeoLocation? LastKnownLocation { get; set; }
    public DateTime? LocationUpdatedAt { get; set; }
    public List<string> Certifications { get; set; } = [];
    public double MaxResponseRadiusKm { get; set; } = 25.0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class CheckIn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ResponderId { get; set; }
    public Guid IncidentId { get; set; }
    public CheckInType Type { get; set; }
    public GeoLocation Location { get; set; } = new(0, 0);
    public string? Notes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Request/response records

public record RegisterResponderRequest(
    string Name,
    string Email,
    ResponderType Type,
    string? BadgeNumber = null,
    string? Phone = null,
    List<string>? Certifications = null,
    double MaxResponseRadiusKm = 25.0);

public record UpdateLocationRequest(
    double Latitude,
    double Longitude,
    double? Accuracy = null);

public record UpdateStatusRequest(ResponderStatus Status);

public record CreateCheckInRequest(
    Guid IncidentId,
    CheckInType Type,
    double Latitude,
    double Longitude,
    string? Notes = null);

public record NearbyResponderQuery(
    double Latitude,
    double Longitude,
    double RadiusKm = 10.0,
    ResponderType? Type = null,
    bool AvailableOnly = true);

public record ResponderListResponse(
    List<Responder> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record ResponderSummary(
    Guid Id,
    string Name,
    ResponderType Type,
    ResponderStatus Status,
    double? DistanceKm,
    GeoLocation? Location);
