namespace TheWatch.P1.CoreGateway.Gateway;

public enum ServiceStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Offline
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

