using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using TheWatch.Geospatial.Spatial;

namespace TheWatch.Geospatial.Services;

/// <summary>
/// PostGIS-backed implementation of geospatial intelligence caching and inferencing.
/// Uses ST_DWithin spatial queries to find relevant intel near a location and derives
/// composite threat assessments to support Watch situational planning.
/// </summary>
public class IntelService : IIntelService
{
    private readonly GeospatialDbContext _db;
    private readonly GeometryFactory _gf;

    public IntelService(GeospatialDbContext db)
    {
        _db = db;
        _gf = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    private Point MakePoint(double lon, double lat) => _gf.CreatePoint(new Coordinate(lon, lat));

    public async Task<IntelEntry> IngestEntryAsync(IngestIntelEntryRequest req)
    {
        var entry = new IntelEntry
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Summary = req.Summary,
            SourceType = req.SourceType,
            SourceUrl = req.SourceUrl,
            SourceName = req.SourceName,
            Location = MakePoint(req.Longitude, req.Latitude),
            RadiusMeters = req.RadiusMeters,
            Category = req.Category,
            ThreatLevel = req.ThreatLevel,
            ConfidenceScore = Math.Clamp(req.ConfidenceScore, 0.0, 1.0),
            Tags = req.Tags ?? new Dictionary<string, string>(),
            IsActive = true,
            PublishedAt = req.PublishedAt,
            IngestedAt = DateTimeOffset.UtcNow,
            ExpiresAt = req.ExpiresAt
        };

        _db.IntelEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task<List<IntelEntry>> QueryEntriesNearAsync(
        double longitude, double latitude, double radiusMeters,
        IntelCategory? category = null, IntelThreatLevel? minThreatLevel = null, int count = 20)
    {
        var point = MakePoint(longitude, latitude);
        var now = DateTimeOffset.UtcNow;

        var query = _db.IntelEntries.Where(e =>
            e.IsActive &&
            (e.ExpiresAt == null || e.ExpiresAt > now) &&
            e.Location.IsWithinDistance(point, radiusMeters));

        if (category.HasValue)
            query = query.Where(e => e.Category == category.Value);

        if (minThreatLevel.HasValue)
        {
            // Enumerate qualifying threat levels since enum-as-string doesn't support >= in SQL
            var validLevels = Enum.GetValues<IntelThreatLevel>()
                .Where(t => t >= minThreatLevel.Value)
                .ToList();
            query = query.Where(e => validLevels.Contains(e.ThreatLevel));
        }

        return await query
            .OrderByDescending(e => e.ThreatLevel)
            .ThenByDescending(e => e.ConfidenceScore)
            .ThenBy(e => e.Location.Distance(point))
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<IntelInference>> GetInferencesNearAsync(
        double longitude, double latitude, double radiusMeters)
    {
        var point = MakePoint(longitude, latitude);
        var now = DateTimeOffset.UtcNow;

        return await _db.IntelInferences.Where(i =>
            i.IsActive &&
            (i.ExpiresAt == null || i.ExpiresAt > now) &&
            i.Location.IsWithinDistance(point, radiusMeters))
            .OrderByDescending(i => i.ThreatLevel)
            .ThenByDescending(i => i.ConfidenceScore)
            .ToListAsync();
    }

    public async Task<IntelInference> GenerateInferenceAsync(
        double longitude, double latitude, double radiusMeters, IntelCategory category)
    {
        var point = MakePoint(longitude, latitude);
        var now = DateTimeOffset.UtcNow;

        var entries = await _db.IntelEntries.Where(e =>
            e.IsActive &&
            e.Category == category &&
            (e.ExpiresAt == null || e.ExpiresAt > now) &&
            e.Location.IsWithinDistance(point, radiusMeters))
            .ToListAsync();

        var threatLevel = entries.Count == 0
            ? IntelThreatLevel.Informational
            : entries.Max(e => e.ThreatLevel);

        var confidence = entries.Count == 0
            ? 0.0
            : entries.Average(e => e.ConfidenceScore);

        var topSource = entries.Count == 0
            ? "none"
            : entries.OrderByDescending(e => e.ThreatLevel).First().SourceName;

        var summary = entries.Count == 0
            ? $"No intel entries found within {radiusMeters:N0} m for category {category}."
            : $"{category} assessment: {threatLevel} threat level based on {entries.Count} source(s) " +
              $"within {radiusMeters:N0} m. Avg confidence: {confidence:P0}. Top source: {topSource}.";

        var inference = new IntelInference
        {
            Id = Guid.NewGuid(),
            Location = point,
            RadiusMeters = radiusMeters,
            Category = category,
            ThreatLevel = threatLevel,
            Summary = summary,
            ConfidenceScore = confidence,
            SupportingEntryCount = entries.Count,
            IsActive = true,
            GeneratedAt = now,
            ExpiresAt = now.AddHours(6)
        };

        _db.IntelInferences.Add(inference);
        await _db.SaveChangesAsync();
        return inference;
    }
}
