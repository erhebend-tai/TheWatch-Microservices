namespace TheWatch.P5.AuthSecurity.Models;

public enum PermissionType { Camera, Location, Microphone, Contacts, Storage, Notifications, Bluetooth, Nfc, BackgroundLocation, HealthData }
public enum PermissionState { Granted, Denied, Restricted, NotDetermined, PermanentlyDenied }

public class DevicePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Guid UserId { get; set; }
    public PermissionType PermissionType { get; set; }
    public PermissionState State { get; set; } = PermissionState.NotDetermined;
    public bool IsRequired { get; set; }
    public string? Reason { get; set; }
    public DateTime? GrantedAt { get; set; }
    public DateTime? DeniedAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
