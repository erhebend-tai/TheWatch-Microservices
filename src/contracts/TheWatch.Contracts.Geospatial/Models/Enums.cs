namespace TheWatch.Contracts.Geospatial.Models;

public enum GeoLocationType { GPS, CellTower, WiFi, Manual, Estimated }
public enum GeoZoneType { SafeZone, DangerZone, IncidentPerimeter, EvacuationZone, SearchArea, Geofence, Jurisdiction }
public enum ZoneSeverity { Low, Medium, High, Critical, Catastrophic }
public enum TrackingStatus { Active, Idle, Offline, Emergency }
public enum GeofenceAlertType { Entry, Exit, Both }
