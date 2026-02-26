namespace TheWatch.P5.AuthSecurity.Models;

public class PermissionAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Guid UserId { get; set; }
    public PermissionType PermissionType { get; set; }
    public PermissionState OldState { get; set; }
    public PermissionState NewState { get; set; }
    public string? ChangeSource { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
