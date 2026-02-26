namespace TheWatch.Contracts.VoiceEmergency.Models;

public record CreateIncidentRequest(EmergencyType Type, string Description, LocationDto Location, Guid ReporterId, string? ReporterName = null, string? ReporterPhone = null, int Severity = 3, List<string>? Tags = null);
public record UpdateIncidentStatusRequest(IncidentStatus Status, string? Reason = null);
public record CreateDispatchRequest(Guid IncidentId, double RadiusKm = 5.0, int RespondersRequested = 8);
public record ExpandRadiusRequest(double AdditionalKm = 5.0);
