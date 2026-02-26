namespace TheWatch.Contracts.FamilyHealth.Models;

public record CreateFamilyGroupRequest(string Name);
public record AddMemberRequest(string Name, FamilyRole Role, string? Email = null, string? Phone = null);
public record CreateCheckInRequest(CheckInStatus Status, string? Message = null, double? Latitude = null, double? Longitude = null);
public record RecordVitalRequest(VitalType Type, double Value, string? Unit = null);
