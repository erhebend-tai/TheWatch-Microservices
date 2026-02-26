using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Gcp;

/// <summary>
/// No-op content analysis provider for development/testing.
/// Returns safe/empty results. Replace with GoogleVisionProvider when implementing.
/// </summary>
public class NoOpContentAnalysisProvider : IContentAnalysisProvider
{
    private readonly ILogger<NoOpContentAnalysisProvider> _logger;

    public NoOpContentAnalysisProvider(ILogger<NoOpContentAnalysisProvider> logger)
    {
        _logger = logger;
    }

    public bool IsConfigured => false;

    public Task<ContentAnalysisResult> AnalyzeImageAsync(
        byte[] imageData, string mimeType, CancellationToken ct)
    {
        _logger.LogDebug("NoOp Vision: AnalyzeImageAsync called with {Bytes} bytes", imageData.Length);
        return Task.FromResult(new ContentAnalysisResult { IsSafe = true });
    }

    public Task<ContentAnalysisResult> AnalyzeImageUrlAsync(string imageUrl, CancellationToken ct)
    {
        _logger.LogDebug("NoOp Vision: AnalyzeImageUrlAsync called for {Url}", imageUrl);
        return Task.FromResult(new ContentAnalysisResult { IsSafe = true });
    }

    public Task<TextExtractionResult> ExtractTextAsync(
        byte[] imageData, string mimeType, CancellationToken ct)
    {
        _logger.LogDebug("NoOp Vision: ExtractTextAsync called");
        return Task.FromResult(new TextExtractionResult());
    }

    public Task<List<DetectedLabel>> DetectLabelsAsync(
        byte[] imageData, int maxResults, CancellationToken ct)
    {
        _logger.LogDebug("NoOp Vision: DetectLabelsAsync called");
        return Task.FromResult(new List<DetectedLabel>());
    }
}
