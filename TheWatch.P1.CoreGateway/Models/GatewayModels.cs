namespace TheWatch.P1.CoreGateway.Gateway;

public enum ServiceStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Offline
}

public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public Dictionary<string, string> Preferences { get; set; } = new();
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
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public ServiceStatus Status { get; set; } = ServiceStatus.Offline;
    public DateTime? LastHealthCheck { get; set; }
}

// Request/response records

public record CreateProfileRequest(
    string DisplayName,
    string Email,
    string? Phone = null,
    string? AvatarUrl = null);

public record UpdateProfileRequest(
    string? DisplayName = null,
    string? Phone = null,
    string? AvatarUrl = null);

public record SetConfigRequest(string Key, string Value, string? Description = null);

public record ProfileListResponse(List<UserProfile> Items, int TotalCount);
public record ServiceHealthSummary(List<ServiceRegistration> Services, int HealthyCount, int TotalCount);
