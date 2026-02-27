namespace TheWatch.P5.AuthSecurity.Models;

public class DeviceTrust
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public int LoginCount { get; set; }
    public int TrustScore { get; set; }
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsTrusted { get; set; }
}
