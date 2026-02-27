namespace MicroGen.Core.Models;

/// <summary>
/// Result of generating a single microservice.
/// </summary>
public sealed record GenerationResult
{
    public required string ServiceName { get; init; }
    public required string DomainName { get; init; }
    public required string OutputPath { get; init; }
    public bool Success { get; init; }
    public List<string> GeneratedFiles { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public List<string> Errors { get; init; } = [];
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Aggregate result of a full generation run.
/// </summary>
public sealed record GenerationSummary
{
    public List<GenerationResult> Results { get; init; } = [];
    public int TotalServices => Results.Count;
    public int SuccessCount => Results.Count(r => r.Success);
    public int FailureCount => Results.Count(r => !r.Success);
    public int TotalFiles => Results.Sum(r => r.GeneratedFiles.Count);
    public TimeSpan TotalDuration { get; init; }
}
