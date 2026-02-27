namespace TheWatch.Contracts.DisasterRelief.Models;

public record CreateDisasterEventRequest(DisasterType Type, string Name, string? Description, double Latitude, double Longitude, double RadiusKm = 10.0, int Severity = 3);
public record UpdateEventStatusRequest(EventStatus Status);
public record CreateShelterRequest(string Name, double Latitude, double Longitude, int Capacity, string? ContactPhone = null, List<string>? Amenities = null, Guid? DisasterEventId = null);
public record UpdateOccupancyRequest(int CurrentOccupancy);
public record DonateResourceRequest(ResourceCategory Category, string Name, int Quantity, string? Unit, double Latitude, double Longitude, Guid? DonorId = null, Guid? DisasterEventId = null);
public record CreateResourceRequestRecord(Guid RequesterId, ResourceCategory Category, int Quantity, RequestPriority Priority, double Latitude, double Longitude, Guid? DisasterEventId = null);
public record CreateEvacuationRouteRequest(Guid DisasterEventId, double OriginLat, double OriginLon, double DestLat, double DestLon, double DistanceKm, int EstimatedTimeMinutes, string? Description = null);
