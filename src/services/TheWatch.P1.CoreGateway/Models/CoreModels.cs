namespace TheWatch.P1.CoreGateway.Core;

public enum UserRole
{
    Citizen,
    Responder,
    Admin,
    SystemOperator
}

public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Citizen;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public Dictionary<string, string> Preferences { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PlatformConfig
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ServiceRegistration
{
    public string ServiceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
}

// Request/response records

public record CreateProfileRequest(
    string DisplayName,
    string Email,
    string? Phone = null,
    UserRole Role = UserRole.Citizen);

public record UpdateProfileRequest(
    string? DisplayName = null,
    string? Phone = null,
    double? Latitude = null,
    double? Longitude = null);

public record SetPreferenceRequest(string Key, string Value);

public record SetConfigRequest(string Key, string Value, string? Description = null);

public record ProfileListResponse(
    List<UserProfile> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record ServiceHealthSummary(
    List<ServiceRegistration> Services,
    int HealthyCount,
    int UnhealthyCount);
