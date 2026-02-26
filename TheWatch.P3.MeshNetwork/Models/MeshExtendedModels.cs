namespace TheWatch.P3.MeshNetwork.Mesh;

// Extended enums
public enum BroadcastType { Standard, Emergency, Priority, System, Relay }
public enum BroadcastStatus { Pending, Broadcasting, Delivered, Failed, Expired, Cancelled }
public enum EncryptionType { Aes256, ChaCha20, None }
public enum RelayType { Forward, Store, Flood, Selective }
public enum RelayStatus { Pending, InProgress, Completed, Failed, Skipped }
public enum ApprovalStatus { Pending, Approved, Denied, Revoked, Expired }
public enum ApprovalType { Peer, Admin, Automatic, Emergency }
public enum VerificationMethod { Manual, QrCode, Nfc, SharedSecret, Challenge }
public enum ActivationStatus { Standby, Activating, Active, Deactivating, Deactivated }
public enum FilterType { Regex, Keyword, Category, Ai, Blocklist }
public enum FilterAction { Block, Redact, Flag, Allow, Quarantine }
public enum ReviewStatus { Pending, Approved, Rejected, Escalated }
public enum ProcessingStatus { Queued, Processing, Transmitted, Completed, Failed }
public enum QueueStatus { Queued, Processing, Completed, Failed, Expired, DeadLettered }
public enum TruncationMethod { SmartTrim, HeadTrim, TailTrim, Compress, Summary }
public enum EmergencyChannelStatus { Active, Inactive, Full, Maintenance }
public enum QuotaPeriod { Hourly, Daily, Weekly, Monthly }
public enum TapResponseStatus { Detected, Confirmed, Broadcasting, Acknowledged, Resolved, FalseAlarm, Cancelled }
public enum MeshNodeType { Peer, Relay, Gateway, Coordinator, Emergency }
public enum MeshRole { Standard, Relay, Gateway, Coordinator, Observer }

public class PhraseBroadcast
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SenderNodeId { get; set; }
    public string PhraseText { get; set; } = string.Empty;
    public BroadcastType BroadcastType { get; set; } = BroadcastType.Standard;
    public EncryptionType EncryptionType { get; set; } = EncryptionType.Aes256;
    public double BroadcastRadiusMeters { get; set; } = 100.0;
    public int HopCount { get; set; }
    public int MaxHops { get; set; } = 5;
    public int TtlSeconds { get; set; } = 300;
    public int PriorityLevel { get; set; } = 5;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool DeliveryConfirmation { get; set; }
    public int ConfirmedRecipients { get; set; }
    public Guid? TargetRecipientId { get; set; }
    public bool IsGroupBroadcast { get; set; }
    public Guid? GroupId { get; set; }
    public bool ContentFilterPassed { get; set; } = true;
    public Guid? FilterId { get; set; }
    public Guid? TemplateId { get; set; }
    public bool Truncated { get; set; }
    public BroadcastStatus BroadcastStatus { get; set; } = BroadcastStatus.Pending;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string? ErrorMessage { get; set; }
    public DateTime? BroadcastAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class PhraseRelay
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BroadcastId { get; set; }
    public Guid RelayNodeId { get; set; }
    public Guid SourceNodeId { get; set; }
    public Guid? DestinationNodeId { get; set; }
    public int RelaySequence { get; set; } = 1;
    public int HopNumber { get; set; }
    public RelayType RelayType { get; set; } = RelayType.Forward;
    public double? SignalStrengthInDbm { get; set; }
    public double? SignalStrengthOutDbm { get; set; }
    public double? LatencyMs { get; set; }
    public string? ProtocolUsed { get; set; }
    public double? RelayLatitude { get; set; }
    public double? RelayLongitude { get; set; }
    public RelayStatus RelayStatus { get; set; } = RelayStatus.Pending;
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public bool ContentVerified { get; set; }
    public DateTime? RelayedAt { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class DeviceApproval
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestingNodeId { get; set; }
    public Guid ApprovingNodeId { get; set; }
    public ApprovalType ApprovalType { get; set; } = ApprovalType.Peer;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public int ApprovalLevel { get; set; } = 1;
    public VerificationMethod VerificationMethod { get; set; } = VerificationMethod.Manual;
    public double TrustScoreGranted { get; set; } = 0.5;
    public string? PermissionsGranted { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? RevocationReason { get; set; }
    public string? RequestMessage { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class EmergencyMeshNetwork
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NetworkName { get; set; } = string.Empty;
    public string NetworkCode { get; set; } = string.Empty;
    public string EmergencyType { get; set; } = string.Empty;
    public int SeverityLevel { get; set; } = 3;
    public ActivationStatus ActivationStatus { get; set; } = ActivationStatus.Standby;
    public Guid? CoordinatorNodeId { get; set; }
    public Guid? BackupCoordinatorId { get; set; }
    public int MaxNodes { get; set; } = 100;
    public int CurrentNodeCount { get; set; }
    public double? CenterLatitude { get; set; }
    public double? CenterLongitude { get; set; }
    public double? CoverageRadiusKm { get; set; }
    public string? FrequencyBand { get; set; }
    public int MessageSizeLimitBytes { get; set; } = 1024;
    public bool RequiresAuthentication { get; set; } = true;
    public bool AutoApproveEmergency { get; set; } = true;
    public string ProtocolVersion { get; set; } = "1.0";
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public double? EstimatedDurationHrs { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class ContentFilter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FilterName { get; set; } = string.Empty;
    public FilterType FilterType { get; set; }
    public string FilterCategory { get; set; } = string.Empty;
    public string PatternValue { get; set; } = string.Empty;
    public string? ReplacementValue { get; set; }
    public FilterAction ActionOnMatch { get; set; } = FilterAction.Block;
    public int SeverityLevel { get; set; } = 3;
    public double ConfidenceThreshold { get; set; } = 0.8;
    public bool CaseSensitive { get; set; }
    public bool AppliesToEmergency { get; set; }
    public Guid? NetworkId { get; set; }
    public int PriorityOrder { get; set; } = 100;
    public long HitCount { get; set; }
    public long FalsePositiveCount { get; set; }
    public DateTime? LastMatchedAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class FilteredMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BroadcastId { get; set; }
    public Guid FilterId { get; set; }
    public FilterAction FilterAction { get; set; } = FilterAction.Block;
    public string FilterReason { get; set; } = string.Empty;
    public string? MatchedPattern { get; set; }
    public string? MatchedCategory { get; set; }
    public double ConfidenceScore { get; set; } = 1.0;
    public int SeverityLevel { get; set; } = 3;
    public string? RedactedContent { get; set; }
    public Guid? ReviewerNodeId { get; set; }
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Pending;
    public string? ReviewNotes { get; set; }
    public bool Appealed { get; set; }
    public string? AppealResult { get; set; }
    public DateTime FilteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class AllowedMessageType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string TypeCategory { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxPayloadBytes { get; set; } = 512;
    public bool RequiresEncryption { get; set; } = true;
    public bool RequiresAuthentication { get; set; } = true;
    public double MinTrustScore { get; set; }
    public int PriorityDefault { get; set; } = 5;
    public int TtlDefaultSeconds { get; set; } = 300;
    public bool AllowedInEmergency { get; set; } = true;
    public bool RequiresApproval { get; set; }
    public bool ContentFilterRequired { get; set; } = true;
    public double BandwidthWeight { get; set; } = 1.0;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; } = 100;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class MessageTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCategory { get; set; } = string.Empty;
    public string TemplateBody { get; set; } = string.Empty;
    public string? TemplateBodyShort { get; set; }
    public string? PlaceholderSchema { get; set; }
    public Guid? MessageTypeId { get; set; }
    public int DefaultPriority { get; set; } = 4;
    public int DefaultTtlSeconds { get; set; } = 300;
    public bool RequiresLocation { get; set; }
    public int MaxLength { get; set; } = 500;
    public string LanguageCode { get; set; } = "en";
    public string? IconName { get; set; }
    public string? ColorCode { get; set; }
    public long UsageCount { get; set; }
    public bool IsSystemTemplate { get; set; }
    public bool IsEmergencyTemplate { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; } = 100;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class PriorityMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BroadcastId { get; set; }
    public int PriorityLevel { get; set; }
    public string PriorityReason { get; set; } = string.Empty;
    public int EscalationLevel { get; set; }
    public int MaxEscalation { get; set; } = 3;
    public bool AutoEscalate { get; set; }
    public int EscalationIntervalSec { get; set; } = 60;
    public bool AcknowledgmentRequired { get; set; }
    public Guid? AcknowledgedByNodeId { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool OverrideFilters { get; set; }
    public bool OverrideBandwidth { get; set; }
    public bool OverrideQueue { get; set; } = true;
    public Guid? DedicatedChannelId { get; set; }
    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Queued;
    public DateTime? FirstTransmittedAt { get; set; }
    public DateTime? LastRetransmitAt { get; set; }
    public int RetransmitCount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class MessageTruncation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BroadcastId { get; set; }
    public int OriginalLengthBytes { get; set; }
    public int TruncatedLengthBytes { get; set; }
    public TruncationMethod TruncationMethod { get; set; } = TruncationMethod.SmartTrim;
    public string TruncationReason { get; set; } = string.Empty;
    public string? OriginalContentRef { get; set; }
    public double? CompressionRatio { get; set; }
    public double ContentPreservedPct { get; set; } = 100.0;
    public string? KeyPhrasesPreserved { get; set; }
    public DateTime TruncatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class EmergencyChannel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ChannelCode { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelType { get; set; } = "Emergency";
    public Guid? NetworkId { get; set; }
    public string? FrequencyBand { get; set; }
    public int? ChannelNumber { get; set; }
    public bool EncryptionRequired { get; set; } = true;
    public int MaxParticipants { get; set; } = 50;
    public int CurrentParticipants { get; set; }
    public double? BandwidthAllocatedKbps { get; set; }
    public int PriorityFloor { get; set; } = 3;
    public int MessageSizeLimit { get; set; } = 512;
    public bool RequiresAuthentication { get; set; } = true;
    public double MinTrustScore { get; set; } = 0.3;
    public bool AutoJoinEmergency { get; set; } = true;
    public EmergencyChannelStatus ChannelStatus { get; set; } = EmergencyChannelStatus.Active;
    public Guid? ModeratorNodeId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class BandwidthAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NodeId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? NetworkId { get; set; }
    public string AllocationType { get; set; } = "Standard";
    public double AllocatedKbps { get; set; }
    public double UsedKbps { get; set; }
    public double PeakKbps { get; set; }
    public double MinGuaranteedKbps { get; set; }
    public double? MaxBurstKbps { get; set; }
    public QuotaPeriod QuotaPeriod { get; set; } = QuotaPeriod.Hourly;
    public double? QuotaLimitMb { get; set; }
    public double QuotaUsedMb { get; set; }
    public int PriorityLevel { get; set; } = 5;
    public double ThrottleAtPct { get; set; } = 80.0;
    public double BlockAtPct { get; set; } = 100.0;
    public bool IsThrottled { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidUntil { get; set; }
    public DateTime? LastMeasuredAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class MessageQueueEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? BroadcastId { get; set; }
    public string QueueName { get; set; } = "DEFAULT";
    public int PayloadSizeBytes { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public int PriorityLevel { get; set; } = 5;
    public Guid SenderNodeId { get; set; }
    public Guid? TargetNodeId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? NetworkId { get; set; }
    public QueueStatus QueueStatus { get; set; } = QueueStatus.Queued;
    public int DequeueCount { get; set; }
    public int MaxDequeueCount { get; set; } = 5;
    public int VisibilityTimeoutSec { get; set; } = 30;
    public DateTime VisibleAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeferredUntil { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime FirstEnqueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastDequeuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class TapEmergency
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NodeId { get; set; }
    public string DetectedPattern { get; set; } = string.Empty;
    public int TapCount { get; set; }
    public int TapDurationMs { get; set; }
    public double AverageIntervalMs { get; set; }
    public double PatternConfidence { get; set; }
    public string EmergencyType { get; set; } = string.Empty;
    public int EmergencySeverity { get; set; } = 2;
    public bool AutoBroadcast { get; set; } = true;
    public Guid? BroadcastId { get; set; }
    public Guid? NetworkId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool Acknowledged { get; set; }
    public Guid? AcknowledgedByNodeId { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool Cancelled { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public bool FalseAlarm { get; set; }
    public TapResponseStatus ResponseStatus { get; set; } = TapResponseStatus.Detected;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class NodeConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NodeAId { get; set; }
    public Guid NodeBId { get; set; }
    public string ConnectionType { get; set; } = "Bluetooth";
    public double? SignalStrengthDbm { get; set; }
    public double? LatencyMs { get; set; }
    public double? BandwidthKbps { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EstablishedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DisconnectedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class MeshAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? NodeId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
