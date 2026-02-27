namespace TheWatch.P5.AuthSecurity.Models;

public enum RequestOutcome { Pending, Granted, Denied, Deferred, Expired }

public class PermissionRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Guid UserId { get; set; }
    public PermissionType PermissionType { get; set; }
    public string Justification { get; set; } = string.Empty;
    public string? FeatureContext { get; set; }
    public RequestOutcome Outcome { get; set; } = RequestOutcome.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public int PromptCount { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
