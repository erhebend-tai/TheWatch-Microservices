using TheWatch.Geospatial.Spatial;

namespace TheWatch.Geospatial.Services;

/// <summary>
/// Geospatial intelligence caching and inferencing service.
/// Ingests encyclopedic, news, and other external data with geo-context,
/// and generates situational inferences to support Watch planning.
/// </summary>
public interface IIntelService
{
    /// <summary>Ingest a new intelligence entry (news, encyclopedic, field report, etc.).</summary>
    Task<IntelEntry> IngestEntryAsync(IngestIntelEntryRequest req);

    /// <summary>Query cached intel entries near a geographic point, optionally filtered by category and minimum threat level.</summary>
    Task<List<IntelEntry>> QueryEntriesNearAsync(
        double longitude, double latitude, double radiusMeters,
        IntelCategory? category = null, IntelThreatLevel? minThreatLevel = null, int count = 20);

    /// <summary>Get existing active inferences near a geographic point.</summary>
    Task<List<IntelInference>> GetInferencesNearAsync(double longitude, double latitude, double radiusMeters);

    /// <summary>
    /// Analyze cached intel entries near the given location for a specific category
    /// and generate a new geospatial threat inference.
    /// </summary>
    Task<IntelInference> GenerateInferenceAsync(
        double longitude, double latitude, double radiusMeters, IntelCategory category);
}

// ─── Request DTOs ───

public record IngestIntelEntryRequest(
    string Title,
    string Summary,
    /// <summary>News | Encyclopedia | SocialMedia | FieldReport | Sensor</summary>
    string SourceType,
    string SourceUrl,
    string SourceName,
    double Longitude,
    double Latitude,
    /// <summary>Radius (meters) of geographic relevance for this entry.</summary>
    double RadiusMeters,
    IntelCategory Category,
    IntelThreatLevel ThreatLevel,
    /// <summary>Analyst confidence 0.0–1.0.</summary>
    double ConfidenceScore,
    DateTimeOffset PublishedAt,
    DateTimeOffset? ExpiresAt = null,
    Dictionary<string, string>? Tags = null);

public record GenerateInferenceRequest(
    double Longitude,
    double Latitude,
    double RadiusMeters,
    IntelCategory Category);
