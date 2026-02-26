using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Compliance;

/// <summary>
/// GDPR right-to-erasure (Article 17) implementation across all services.
/// Provides cascade delete with audit trail. Each service registers its
/// erasure handlers; this coordinator orchestrates deletion across all of them.
/// </summary>
public interface IGdprErasureService
{
    /// <summary>Register a service-specific erasure handler.</summary>
    void RegisterHandler(string serviceName, IErasureHandler handler);

    /// <summary>
    /// Execute right-to-erasure for a user across all registered services.
    /// Returns a detailed report of what was deleted.
    /// </summary>
    Task<ErasureReport> ExecuteErasureAsync(Guid userId, string requestedBy, string? reason = null);

    /// <summary>Check if an erasure request is pending or completed for a user.</summary>
    Task<ErasureStatus?> GetErasureStatusAsync(Guid userId);
}

/// <summary>Service-specific handler for deleting a user's data.</summary>
public interface IErasureHandler
{
    /// <summary>Delete all data belonging to the specified user.</summary>
    Task<ErasureResult> EraseUserDataAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Get a count of records that would be affected.</summary>
    Task<int> CountUserRecordsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public record ErasureReport
{
    public Guid ErasureId { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string RequestedBy { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public ErasureOutcome Outcome { get; init; }
    public IReadOnlyList<ErasureResult> ServiceResults { get; init; } = [];
    public int TotalRecordsDeleted { get; init; }
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; init; }
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - RequestedAt : null;
}

public record ErasureResult
{
    public string ServiceName { get; init; } = string.Empty;
    public int RecordsDeleted { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
    public TimeSpan Duration { get; init; }
}

public record ErasureStatus
{
    public Guid UserId { get; init; }
    public ErasureOutcome Outcome { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public enum ErasureOutcome
{
    Pending,
    Completed,
    PartiallyCompleted,  // Some services failed
    Failed,
    Rejected             // Legal hold or regulatory retention
}

public class GdprErasureService : IGdprErasureService
{
    private readonly ILogger<GdprErasureService> _logger;
    private readonly Dictionary<string, IErasureHandler> _handlers = new();
    private readonly List<ErasureReport> _reports = new();

    public GdprErasureService(ILogger<GdprErasureService> logger)
    {
        _logger = logger;
    }

    public void RegisterHandler(string serviceName, IErasureHandler handler)
    {
        _handlers[serviceName] = handler;
        _logger.LogInformation("GDPR erasure handler registered: {ServiceName}", serviceName);
    }

    public async Task<ErasureReport> ExecuteErasureAsync(Guid userId, string requestedBy, string? reason = null)
    {
        _logger.LogWarning("GDPR erasure initiated for user {UserId} by {RequestedBy}: {Reason}",
            userId, requestedBy, reason ?? "No reason provided");

        var results = new List<ErasureResult>();
        var startTime = DateTime.UtcNow;

        foreach (var (serviceName, handler) in _handlers)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await handler.EraseUserDataAsync(userId);
                sw.Stop();
                results.Add(result with { ServiceName = serviceName, Duration = sw.Elapsed });

                _logger.LogInformation(
                    "GDPR erasure {Service}: {Count} records deleted in {Duration}ms",
                    serviceName, result.RecordsDeleted, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "GDPR erasure failed for service {Service}", serviceName);
                results.Add(new ErasureResult
                {
                    ServiceName = serviceName,
                    Success = false,
                    Error = ex.Message,
                    Duration = sw.Elapsed
                });
            }
        }

        var allSuccess = results.All(r => r.Success);
        var anySuccess = results.Any(r => r.Success);
        var outcome = allSuccess ? ErasureOutcome.Completed :
                      anySuccess ? ErasureOutcome.PartiallyCompleted : ErasureOutcome.Failed;

        var report = new ErasureReport
        {
            UserId = userId,
            RequestedBy = requestedBy,
            Reason = reason,
            Outcome = outcome,
            ServiceResults = results,
            TotalRecordsDeleted = results.Sum(r => r.RecordsDeleted),
            CompletedAt = DateTime.UtcNow
        };

        _reports.Add(report);

        _logger.LogWarning(
            "GDPR erasure complete for user {UserId}: outcome={Outcome}, total={Total} records deleted across {Services} services",
            userId, outcome, report.TotalRecordsDeleted, results.Count);

        return report;
    }

    public Task<ErasureStatus?> GetErasureStatusAsync(Guid userId)
    {
        var report = _reports.Where(r => r.UserId == userId).OrderByDescending(r => r.RequestedAt).FirstOrDefault();
        if (report is null) return Task.FromResult<ErasureStatus?>(null);

        return Task.FromResult<ErasureStatus?>(new ErasureStatus
        {
            UserId = userId,
            Outcome = report.Outcome,
            RequestedAt = report.RequestedAt,
            CompletedAt = report.CompletedAt
        });
    }
}
