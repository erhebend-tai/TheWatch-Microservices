namespace TheWatch.Shared.Contracts;

public record HealthResponse(
    string Service,
    string Program,
    string Status,
    DateTime Timestamp,
    Dictionary<string, object>? Details = null);
