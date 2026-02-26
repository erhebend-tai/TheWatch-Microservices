namespace TheWatch.P5.AuthSecurity.Models;

public class OnboardingProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string CompletedStepsJson { get; set; } = "[]";
    public bool IsComplete { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
