using NetTopologySuite.Geometries;

namespace TheWatch.Geospatial.Spatial;

// ─── Schema: geo_core ───

public class GeoLocation
{
    public Guid Id { get; set; }
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
    public Point Location { get; set; } = null!;
    public double Altitude { get; set; }
    public double Accuracy { get; set; }
    public GeoLocationType LocationType { get; set; }
    public string SourceId { get; set; } = "";
    public string SourceType { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class GeoZone
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Polygon Boundary { get; set; } = null!;
    public Point Centroid { get; set; } = null!;
    public GeoZoneType ZoneType { get; set; }
    public ZoneSeverity Severity { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ParentZoneId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class GeoFence
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Polygon Boundary { get; set; } = null!;
    public double RadiusMeters { get; set; }
    public Point Center { get; set; } = null!;
    public GeoFenceType FenceType { get; set; }
    public Guid OwnerEntityId { get; set; }
    public string OwnerEntityType { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

// ─── Schema: geo_tracking ───

public class TrackedEntity
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = "";
    public Guid ExternalEntityId { get; set; }
    public string DisplayName { get; set; } = "";
    public Point LastKnownLocation { get; set; } = null!;
    public double LastSpeed { get; set; }
    public double LastHeading { get; set; }
    public TrackingStatus Status { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class LocationHistory
{
    public Guid Id { get; set; }
    public Guid TrackedEntityId { get; set; }
    public Point Location { get; set; } = null!;
    public double Speed { get; set; }
    public double Heading { get; set; }
    public double Accuracy { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

public class TrackingSession
{
    public Guid Id { get; set; }
    public Guid TrackedEntityId { get; set; }
    public LineString Path { get; set; } = null!;
    public double TotalDistanceMeters { get; set; }
    public TrackingSessionStatus SessionStatus { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}

// ─── Schema: geo_incidents ───

public class IncidentZone
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public string IncidentType { get; set; } = "";
    public Point EpicenterLocation { get; set; } = null!;
    public Polygon PerimeterBoundary { get; set; } = null!;
    public double InitialRadiusMeters { get; set; }
    public double CurrentRadiusMeters { get; set; }
    public ZoneSeverity Severity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
}

public class ResponderPosition
{
    public Guid Id { get; set; }
    public Guid ResponderId { get; set; }
    public Guid? IncidentId { get; set; }
    public Point Location { get; set; } = null!;
    public double Speed { get; set; }
    public double Heading { get; set; }
    public ResponderDispatchStatus DispatchStatus { get; set; }
    public double? DistanceToIncidentMeters { get; set; }
    public double? EtaMinutes { get; set; }
    public DateTimeOffset ReportedAt { get; set; }
}

public class DispatchRoute
{
    public Guid Id { get; set; }
    public Guid ResponderId { get; set; }
    public Guid IncidentId { get; set; }
    public Point Origin { get; set; } = null!;
    public Point Destination { get; set; } = null!;
    public LineString RoutePath { get; set; } = null!;
    public double DistanceMeters { get; set; }
    public double EstimatedMinutes { get; set; }
    public DispatchRouteStatus RouteStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

// ─── Schema: geo_disaster ───

public class DisasterZone
{
    public Guid Id { get; set; }
    public Guid DisasterEventId { get; set; }
    public string DisasterType { get; set; } = "";
    public MultiPolygon AffectedArea { get; set; } = null!;
    public Point Epicenter { get; set; } = null!;
    public ZoneSeverity Severity { get; set; }
    public double EstimatedAffectedPopulation { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
}

public class EvacuationRoute
{
    public Guid Id { get; set; }
    public Guid DisasterZoneId { get; set; }
    public string RouteName { get; set; } = "";
    public LineString Path { get; set; } = null!;
    public Point StartPoint { get; set; } = null!;
    public Point EndPoint { get; set; } = null!;
    public double DistanceMeters { get; set; }
    public double EstimatedMinutes { get; set; }
    public int CapacityPersons { get; set; }
    public EvacRouteStatus RouteStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ShelterLocation
{
    public Guid Id { get; set; }
    public Guid? DisasterZoneId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public Point Location { get; set; } = null!;
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public ShelterStatus Status { get; set; }
    public bool HasMedical { get; set; }
    public bool HasPower { get; set; }
    public bool HasWater { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

// ─── Schema: geo_family ───

public class FamilyGeofence
{
    public Guid Id { get; set; }
    public Guid FamilyGroupId { get; set; }
    public string Name { get; set; } = "";
    public Polygon Boundary { get; set; } = null!;
    public Point Center { get; set; } = null!;
    public double RadiusMeters { get; set; }
    public GeofenceAlertType AlertType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class FamilyMemberLocation
{
    public Guid Id { get; set; }
    public Guid FamilyGroupId { get; set; }
    public Guid MemberId { get; set; }
    public Point Location { get; set; } = null!;
    public double Accuracy { get; set; }
    public bool IsInsideGeofence { get; set; }
    public Guid? ActiveGeofenceId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

public class GeofenceEvent
{
    public Guid Id { get; set; }
    public Guid FamilyGeofenceId { get; set; }
    public Guid MemberId { get; set; }
    public GeofenceEventType EventType { get; set; }
    public Point Location { get; set; } = null!;
    public DateTimeOffset OccurredAt { get; set; }
}

// ─── Schema: geo_search (spatial indexes + POI) ───

public class PointOfInterest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Address { get; set; } = "";
    public Point Location { get; set; } = null!;
    public string Phone { get; set; } = "";
    public bool IsEmergencyFacility { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class SpatialSearchResult
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public Point Location { get; set; } = null!;
    public double DistanceMeters { get; set; }
    public string Label { get; set; } = "";
    public DateTimeOffset FoundAt { get; set; } = DateTimeOffset.UtcNow;
}

// ─── Schema: geo_intel ───

/// <summary>Cached intelligence entry — news, encyclopedic data, field reports, etc. — with geolocation context.</summary>
public class IntelEntry
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    /// <summary>News | Encyclopedia | SocialMedia | FieldReport | Sensor</summary>
    public string SourceType { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string SourceName { get; set; } = "";
    public Point Location { get; set; } = null!;
    /// <summary>Radius (meters) of geographic relevance for this entry.</summary>
    public double RadiusMeters { get; set; }
    public IntelCategory Category { get; set; }
    public IntelThreatLevel ThreatLevel { get; set; }
    /// <summary>Analyst confidence 0.0–1.0.</summary>
    public double ConfidenceScore { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
}

/// <summary>Derived situational inference generated from nearby IntelEntry records.</summary>
public class IntelInference
{
    public Guid Id { get; set; }
    public Point Location { get; set; } = null!;
    public double RadiusMeters { get; set; }
    public IntelCategory Category { get; set; }
    public IntelThreatLevel ThreatLevel { get; set; }
    public string Summary { get; set; } = "";
    public double ConfidenceScore { get; set; }
    public int SupportingEntryCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
}

// ─── Enums ───

public enum GeoLocationType
{
    GPS, CellTower, WiFi, Manual, Estimated
}

public enum GeoZoneType
{
    SafeZone, DangerZone, IncidentPerimeter, EvacuationZone, SearchArea, Geofence, Jurisdiction
}

public enum ZoneSeverity
{
    Low, Medium, High, Critical, Catastrophic
}

public enum GeoFenceType
{
    Circular, Polygonal, Family, Facility, Custom
}

public enum TrackingStatus
{
    Active, Idle, Offline, Emergency
}

public enum TrackingSessionStatus
{
    InProgress, Completed, Abandoned
}

public enum ResponderDispatchStatus
{
    Available, Dispatched, EnRoute, OnScene, Returning
}

public enum DispatchRouteStatus
{
    Planned, Active, Completed, Cancelled, Rerouted
}

public enum EvacRouteStatus
{
    Open, Congested, Blocked, Closed
}

public enum ShelterStatus
{
    Open, Full, Closed, Preparing
}

public enum GeofenceAlertType
{
    Entry, Exit, Both
}

public enum GeofenceEventType
{
    Entered, Exited
}

public enum IntelCategory
{
    GeneralHazard, CivilUnrest, Crime, NaturalDisaster, PublicHealth, Infrastructure,
    WeatherEvent, HazardousMaterial, PoliticalInstability, Military, Terrorism
}

public enum IntelThreatLevel
{
    Informational, Low, Moderate, Elevated, High, Critical
}
