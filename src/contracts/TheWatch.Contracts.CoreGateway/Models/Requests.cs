namespace TheWatch.Contracts.CoreGateway.Models;

public record CreateProfileRequest(string DisplayName, string Email, string? Phone = null, UserRole Role = UserRole.Citizen);
public record UpdateProfileRequest(string? DisplayName = null, string? Phone = null, double? Latitude = null, double? Longitude = null);
public record SetPreferenceRequest(string Key, string Value);
public record SetConfigRequest(string Key, string Value, string? Description = null);
