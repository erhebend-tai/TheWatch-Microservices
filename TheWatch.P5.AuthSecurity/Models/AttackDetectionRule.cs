namespace TheWatch.P5.AuthSecurity.Models;

public class AttackDetectionRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TechniqueId { get; set; } = string.Empty; // e.g., T1078
    public string TechniqueName { get; set; } = string.Empty;
    public string Tactic { get; set; } = string.Empty; // e.g., Initial Access
    public string DetectionLogic { get; set; } = "{}"; // JSON
    public bool IsEnabled { get; set; } = true;
    public int ThresholdCount { get; set; } = 5;
    public int TimeWindowMinutes { get; set; } = 15;
    public string Severity { get; set; } = "High";
}
