namespace TheWatch.P8.DisasterRelief.Relief;

// Extended enums
public enum VictimStatus { Registered, Sheltered, Evacuated, MedicalCare, Missing, Deceased, Recovered }
public enum NeedUrgency { Low, Medium, High, Critical }
public enum NeedStatus { Open, PartiallyMet, Fulfilled, Cancelled }
public enum MatchStatus { Proposed, Accepted, InTransit, Delivered, Rejected, Expired }
public enum GroupStatus { Forming, Active, Purchasing, Completed, Cancelled }
public enum MemberRole { Organizer, Member, Contributor }
public enum PartnershipStatus { Pending, Active, Suspended, Terminated }
public enum RideStatus { Available, Requested, Matched, InProgress, Completed, Cancelled }
public enum RideMatchStatus { Pending, Accepted, Rejected, Completed, NoShow }
public enum ConditionSeverity { Mild, Moderate, Severe, Critical }
public enum GroupType { MentalHealth, PhysicalRecovery, GriefSupport, ChildrenYouth, PetOwners, General }
public enum ConnectionStatus { Pending, Active, Blocked, Expired }
public enum MessageStatus { Sent, Delivered, Read, Deleted }

public class DisasterVictim
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public Guid DisasterEventId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public VictimStatus Status { get; set; } = VictimStatus.Registered;
    public GeoPoint? LastKnownLocation { get; set; }
    public Guid? CurrentShelterId { get; set; }
    public int HouseholdSize { get; set; } = 1;
    public bool HasMinors { get; set; }
    public bool HasElderly { get; set; }
    public bool HasDisability { get; set; }
    public bool HasPets { get; set; }
    public string? SpecialNeeds { get; set; }
    public string? InsuranceInfo { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class ResourceNeed
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VictimId { get; set; }
    public Guid DisasterEventId { get; set; }
    public ResourceCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? Unit { get; set; }
    public NeedUrgency Urgency { get; set; } = NeedUrgency.Medium;
    public NeedStatus Status { get; set; } = NeedStatus.Open;
    public DateTime? FulfilledAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class ResourceMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ResourceId { get; set; }
    public Guid RequestId { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Proposed;
    public int QuantityMatched { get; set; }
    public double? DistanceKm { get; set; }
    public int? EstimatedDeliveryMin { get; set; }
    public Guid? VolunteerId { get; set; }
    public string? Notes { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class BuyingGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DisasterEventId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ResourceCategory TargetCategory { get; set; }
    public string TargetItem { get; set; } = string.Empty;
    public int TargetQuantity { get; set; }
    public decimal EstimatedUnitPrice { get; set; }
    public decimal CollectedAmount { get; set; }
    public GroupStatus Status { get; set; } = GroupStatus.Forming;
    public Guid OrganizerId { get; set; }
    public int MaxMembers { get; set; } = 50;
    public int CurrentMembers { get; set; }
    public Guid? VendorPartnershipId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime Deadline { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class BuyingGroupMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public MemberRole Role { get; set; } = MemberRole.Member;
    public decimal ContributionAmount { get; set; }
    public int QuantityRequested { get; set; } = 1;
    public bool HasPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class VendorPartnership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DisasterEventId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Website { get; set; }
    public PartnershipStatus Status { get; set; } = PartnershipStatus.Pending;
    public decimal? DiscountPct { get; set; }
    public string? TermsDescription { get; set; }
    public List<string> Categories { get; set; } = [];
    public GeoPoint? Location { get; set; }
    public double? DeliveryRadiusKm { get; set; }
    public bool CanDeliver { get; set; }
    public DateTime? AgreementDate { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class EvacuationRide
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DisasterEventId { get; set; }
    public Guid DriverId { get; set; }
    public string VehicleDescription { get; set; } = string.Empty;
    public int AvailableSeats { get; set; }
    public int ClaimedSeats { get; set; }
    public GeoPoint Origin { get; set; } = new(0, 0);
    public GeoPoint Destination { get; set; } = new(0, 0);
    public DateTime DepartureTime { get; set; }
    public RideStatus Status { get; set; } = RideStatus.Available;
    public bool PetsAllowed { get; set; }
    public bool WheelchairAccessible { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class RideMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RideId { get; set; }
    public Guid PassengerId { get; set; }
    public int SeatsRequested { get; set; } = 1;
    public RideMatchStatus Status { get; set; } = RideMatchStatus.Pending;
    public GeoPoint? PickupLocation { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DroppedOffAt { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class MedicalCondition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VictimId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public ConditionSeverity Severity { get; set; } = ConditionSeverity.Moderate;
    public string? Description { get; set; }
    public string? MedicationsRequired { get; set; }
    public bool RequiresRegularTreatment { get; set; }
    public string? TreatmentSchedule { get; set; }
    public string? AllergiesNotes { get; set; }
    public bool NeedsEvacPriority { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class SupportGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DisasterEventId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GroupType GroupType { get; set; } = GroupType.General;
    public Guid FacilitatorId { get; set; }
    public int MaxMembers { get; set; } = 30;
    public int CurrentMembers { get; set; }
    public bool IsVirtual { get; set; }
    public string? MeetingSchedule { get; set; }
    public string? MeetingLocation { get; set; }
    public string? MeetingLink { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class SupportGroupMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public MemberRole Role { get; set; } = MemberRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class PeerConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequesterId { get; set; }
    public Guid ResponderId { get; set; }
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;
    public string? SharedExperience { get; set; }
    public Guid? DisasterEventId { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? BlockedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class DirectMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public Guid? ConnectionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
