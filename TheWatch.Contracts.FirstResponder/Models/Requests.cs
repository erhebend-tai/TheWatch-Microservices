namespace TheWatch.Contracts.FirstResponder.Models;

public record RegisterResponderRequest(string Name, string Email, ResponderType Type, string? BadgeNumber = null, string? Phone = null, List<string>? Certifications = null, double MaxResponseRadiusKm = 25.0);
public record UpdateLocationRequest(double Latitude, double Longitude, double? Accuracy = null);
public record UpdateStatusRequest(ResponderStatus Status);
public record CreateCheckInRequest(Guid IncidentId, CheckInType Type, double Latitude, double Longitude, string? Notes = null);
public record NearbyResponderQuery(double Latitude, double Longitude, double RadiusKm = 10.0, ResponderType? Type = null, bool AvailableOnly = true);
