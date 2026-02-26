namespace TheWatch.P5.AuthSecurity.Models;

public class ThreatAssessment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Category { get; set; } = string.Empty; // STRIDE category
    public string RuleName { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string Evidence { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
