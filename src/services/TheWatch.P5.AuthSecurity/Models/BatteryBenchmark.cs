namespace TheWatch.P5.AuthSecurity.Models;

public enum BenchmarkStatus { Pending, Running, Completed, Failed }

public class BatteryBenchmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Guid UserId { get; set; }
    public string DeviceModel { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public int StartBatteryPct { get; set; }
    public int EndBatteryPct { get; set; }
    public int DurationMinutes { get; set; }
    public double DrainRatePerHour { get; set; }
    public string? TestProfile { get; set; }
    public BenchmarkStatus Status { get; set; } = BenchmarkStatus.Pending;
    public string? Notes { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
