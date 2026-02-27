namespace TheWatch.Contracts.Geospatial.Models;

public record FindNearbyRequest(double Longitude, double Latitude, int Count = 10, double MaxRadiusMeters = 10000);
public record CreateIncidentZoneRequest(Guid IncidentId, string IncidentType, double Longitude, double Latitude, double RadiusMeters, ZoneSeverity Severity);
public record ExpandZoneRequest(double NewRadiusMeters);
public record CreateGeofenceRequest(Guid FamilyGroupId, string Name, double Longitude, double Latitude, double RadiusMeters, GeofenceAlertType AlertType);
public record RegisterTrackedEntityRequest(string EntityType, Guid ExternalEntityId, string DisplayName, double Longitude, double Latitude);
public record UpdateEntityLocationRequest(double Longitude, double Latitude, double Speed = 0, double Heading = 0);
public record CheckGeofencesRequest(Guid MemberId, Guid FamilyGroupId, double Longitude, double Latitude);
public record PointInZoneRequest(double Longitude, double Latitude, Guid ZoneId);
