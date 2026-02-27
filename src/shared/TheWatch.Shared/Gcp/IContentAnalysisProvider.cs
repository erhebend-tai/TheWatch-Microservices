namespace TheWatch.Shared.Gcp;

/// <summary>
/// Content analysis and moderation provider interface (Item 133).
/// Used for evidence image/video analysis — SafeSearch, label detection, OCR.
///
/// Implementations:
///   - NoOpContentAnalysisProvider: development/testing (returns empty/safe results)
///   - GoogleVisionProvider: Google Cloud Vision API (implement in batch)
///
/// Toggle via Gcp:UseVisionApi = true in appsettings.json.
/// </summary>
public interface IContentAnalysisProvider
{
    /// <summary>
    /// Analyze an image for content moderation (SafeSearch) and classification.
    /// </summary>
    Task<ContentAnalysisResult> AnalyzeImageAsync(
        byte[] imageData, string mimeType = "image/jpeg", CancellationToken ct = default);

    /// <summary>
    /// Analyze an image from a URL.
    /// </summary>
    Task<ContentAnalysisResult> AnalyzeImageUrlAsync(
        string imageUrl, CancellationToken ct = default);

    /// <summary>
    /// Extract text from an image (OCR).
    /// </summary>
    Task<TextExtractionResult> ExtractTextAsync(
        byte[] imageData, string mimeType = "image/jpeg", CancellationToken ct = default);

    /// <summary>
    /// Detect labels/objects in an image for evidence categorization.
    /// </summary>
    Task<List<DetectedLabel>> DetectLabelsAsync(
        byte[] imageData, int maxResults = 10, CancellationToken ct = default);

    /// <summary>
    /// Whether the provider is configured and ready.
    /// </summary>
    bool IsConfigured { get; }
}

// ─── DTOs ───

public record ContentAnalysisResult
{
    public bool IsSafe { get; init; } = true;
    public SafeSearchResult SafeSearch { get; init; } = new();
    public List<DetectedLabel> Labels { get; init; } = [];
    public List<ModerationFlag> Flags { get; init; } = [];
    public string? RawResponse { get; init; }
}

public record SafeSearchResult
{
    /// <summary>Likelihood of adult content (0-5: Unknown/VeryUnlikely/Unlikely/Possible/Likely/VeryLikely).</summary>
    public Likelihood Adult { get; init; }
    public Likelihood Violence { get; init; }
    public Likelihood Racy { get; init; }
    public Likelihood Medical { get; init; }
    public Likelihood Spoof { get; init; }
}

public record ModerationFlag(string Category, float Confidence, string Description);

public record DetectedLabel(string Name, float Confidence, string? Category = null);

public record TextExtractionResult
{
    public string FullText { get; init; } = string.Empty;
    public List<TextBlock> Blocks { get; init; } = [];
    public string DetectedLanguage { get; init; } = string.Empty;
}

public record TextBlock(string Text, float Confidence, BoundingBox? Bounds = null);

public record BoundingBox(int X, int Y, int Width, int Height);

public enum Likelihood
{
    Unknown = 0,
    VeryUnlikely = 1,
    Unlikely = 2,
    Possible = 3,
    Likely = 4,
    VeryLikely = 5
}
