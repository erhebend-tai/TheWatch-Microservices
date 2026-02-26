namespace TheWatch.Contracts.FirstResponder.Models;

public record GeoLocationDto(double Latitude, double Longitude, double? Accuracy = null, DateTime? Timestamp = null);

public class ResponderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BadgeNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public ResponderType Type { get; set; }
    public ResponderStatus Status { get; set; }
    public GeoLocationDto? LastKnownLocation { get; set; }
    public DateTime? LocationUpdatedAt { get; set; }
    public List<string> Certifications { get; set; } = [];
    public double MaxResponseRadiusKm { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class CheckInDto
{
    public Guid Id { get; set; }
    public Guid ResponderId { get; set; }
    public Guid IncidentId { get; set; }
    public CheckInType Type { get; set; }
    public GeoLocationDto Location { get; set; } = new(0, 0);
    public string? Notes { get; set; }
    public DateTime Timestamp { get; set; }
}
