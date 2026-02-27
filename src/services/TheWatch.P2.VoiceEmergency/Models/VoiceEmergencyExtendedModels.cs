namespace TheWatch.P2.VoiceEmergency.Emergency;

// Enums
public enum TriggerType { Panic, Chemical, Medical, Custom, WakeWord, Duress }
public enum ResponseAction { AlertEmergency, AlertContacts, WelfareCheck, ChemicalLookup, Call911, CustomAction }
public enum AlertType { VoiceTrigger, Manual, WelfareEscalation, TripOverdue, ChemicalDetected, ProximityFail, DuressSilent }
public enum AlertStatus { Initiated, Processing, Dispatched, Acknowledged, Resolved, Cancelled, FalseAlarm }
public enum SeverityLevel { Critical, High, Medium, Low }
public enum TripType { Hiking, Driving, Boating, Flying, FieldWork, Commute, Custom }
public enum TripStatus { Planned, Active, Completed, Overdue, Emergency, Cancelled }
public enum WelfareCheckType { Scheduled, TripOverdue, AlertFollowup, Manual, MissedCheckin, ProximityFail }
public enum WelfareCheckStatus { Pending, InProgress, RespondedOk, RespondedHelp, NoResponse, Escalated, Cancelled }
public enum CheckMethod { PhoneCall, Sms, PushNotification, InApp, Proximity, Email }
public enum SubstanceCategory { Industrial, Household, Pharmaceutical, Biological, Agricultural, Radiological, Unknown }
public enum ExposureRoute { Inhalation, Ingestion, Dermal, Ocular, Injection, Unknown }
public enum ExposureSeverity { Minor, Moderate, Severe, LifeThreatening, Unknown }

public class VoiceTrigger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TriggerPhrase { get; set; } = string.Empty;
    public TriggerType TriggerType { get; set; }
    public string LanguageCode { get; set; } = "en-US";
    public decimal ConfidenceThreshold { get; set; } = 0.85m;
    public bool IsActive { get; set; } = true;
    public bool IsSystemDefault { get; set; }
    public int PriorityLevel { get; set; } = 5;
    public int CooldownSeconds { get; set; } = 5;
    public ResponseAction ResponseAction { get; set; }
    public string? CustomActionPayload { get; set; }
    public int FalsePositiveCount { get; set; }
    public int TruePositiveCount { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class VoiceAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TriggerId { get; set; }
    public Guid UserId { get; set; }
    public AlertType AlertType { get; set; }
    public AlertStatus AlertStatus { get; set; } = AlertStatus.Initiated;
    public SeverityLevel SeverityLevel { get; set; } = SeverityLevel.High;
    public string? DetectedPhrase { get; set; }
    public decimal? SpeechConfidence { get; set; }
    public string? AudioClipRef { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public decimal? LocationAccuracyM { get; set; }
    public string? LocationAddress { get; set; }
    public Guid? ResponderId { get; set; }
    public int? ResponseTimeSeconds { get; set; }
    public string? ResolutionNotes { get; set; }
    public int EscalationLevel { get; set; }
    public int MaxEscalationLevel { get; set; } = 3;
    public DateTime? NextEscalationAt { get; set; }
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class TripPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TripName { get; set; } = string.Empty;
    public TripType TripType { get; set; }
    public TripStatus TripStatus { get; set; } = TripStatus.Planned;
    public string DepartureLocation { get; set; } = string.Empty;
    public decimal? DepartureLatitude { get; set; }
    public decimal? DepartureLongitude { get; set; }
    public string DestinationLocation { get; set; } = string.Empty;
    public decimal? DestinationLatitude { get; set; }
    public decimal? DestinationLongitude { get; set; }
    public string? WaypointsJson { get; set; }
    public DateTime PlannedDepartureAt { get; set; }
    public DateTime PlannedArrivalAt { get; set; }
    public DateTime? ActualDepartureAt { get; set; }
    public DateTime? ActualArrivalAt { get; set; }
    public int OverdueThresholdMin { get; set; } = 30;
    public int? CheckinIntervalMin { get; set; }
    public DateTime? LastCheckinAt { get; set; }
    public DateTime? NextCheckinDueAt { get; set; }
    public int MissedCheckinsCount { get; set; }
    public int MaxMissedCheckins { get; set; } = 3;
    public string? EmergencyContactIds { get; set; }
    public string? VehicleDescription { get; set; }
    public string? EquipmentNotes { get; set; }
    public string? RouteDescription { get; set; }
    public string? HazardNotes { get; set; }
    public string? SatelliteDeviceId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class WelfareCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? AlertId { get; set; }
    public Guid? TripId { get; set; }
    public WelfareCheckType CheckType { get; set; }
    public WelfareCheckStatus CheckStatus { get; set; } = WelfareCheckStatus.Pending;
    public CheckMethod CheckMethod { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public int MaxAttempts { get; set; } = 3;
    public int AttemptIntervalMin { get; set; } = 5;
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public string? ResponseText { get; set; }
    public bool ResponseVerified { get; set; }
    public string? VerificationMethod { get; set; }
    public bool EscalationTriggered { get; set; }
    public string? EscalatedTo { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class ChemicalLookup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? AlertId { get; set; }
    public Guid UserId { get; set; }
    public string SubstanceName { get; set; } = string.Empty;
    public string? SubstanceCasNumber { get; set; }
    public SubstanceCategory? SubstanceCategory { get; set; }
    public ExposureRoute ExposureRoute { get; set; }
    public ExposureSeverity ExposureSeverity { get; set; } = ExposureSeverity.Unknown;
    public int? ExposureDurationMin { get; set; }
    public string? EstimatedQuantity { get; set; }
    public string? SymptomsReported { get; set; }
    public string? TreatmentProtocolId { get; set; }
    public bool PoisonControlCalled { get; set; }
    public string? PoisonControlCaseNum { get; set; }
    public bool EmsDispatched { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public string? LocationDescription { get; set; }
    public int PatientCount { get; set; } = 1;
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class EmergencyContact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Relationship { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool ReceiveAlerts { get; set; } = true;
    public bool ReceiveTripUpdates { get; set; }
    public bool CanCancelEmergency { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
