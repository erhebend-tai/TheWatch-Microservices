using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Gcp;

/// <summary>
/// Google Cloud Vision API implementation of IContentAnalysisProvider (Item 133).
///
/// STUB — implement in batch. Wire up:
///   - Google.Cloud.Vision.V1.ImageAnnotatorClient
///   - SafeSearch detection for content moderation
///   - Label detection for evidence categorization
///   - Text detection (OCR) for document evidence
///
/// NuGet: Google.Cloud.Vision.V1
/// Docs: https://cloud.google.com/vision/docs
/// </summary>
public class GoogleVisionProvider : IContentAnalysisProvider
{
    private readonly GcpServiceOptions _options;
    private readonly ILogger<GoogleVisionProvider> _logger;

    public GoogleVisionProvider(GcpServiceOptions options, ILogger<GoogleVisionProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured => true;

    public Task<ContentAnalysisResult> AnalyzeImageAsync(
        byte[] imageData, string mimeType, CancellationToken ct)
    {
        // TODO: Implement with Google.Cloud.Vision.V1.ImageAnnotatorClient
        //
        // var client = await ImageAnnotatorClient.CreateAsync(ct);
        // var image = Image.FromBytes(imageData);
        // var response = await client.DetectSafeSearchAsync(image, ct);
        // Map response.Adult/Violence/Racy/Medical/Spoof to SafeSearchResult

        _logger.LogWarning("GoogleVisionProvider.AnalyzeImageAsync called but not yet implemented");
        throw new NotImplementedException("Google Vision API not yet implemented. Implement in batch.");
    }

    public Task<ContentAnalysisResult> AnalyzeImageUrlAsync(string imageUrl, CancellationToken ct)
    {
        // TODO: Implement with Image.FromUri(imageUrl)
        _logger.LogWarning("GoogleVisionProvider.AnalyzeImageUrlAsync called but not yet implemented");
        throw new NotImplementedException("Google Vision API not yet implemented. Implement in batch.");
    }

    public Task<TextExtractionResult> ExtractTextAsync(
        byte[] imageData, string mimeType, CancellationToken ct)
    {
        // TODO: Implement with client.DetectTextAsync(image, ct)
        _logger.LogWarning("GoogleVisionProvider.ExtractTextAsync called but not yet implemented");
        throw new NotImplementedException("Google Vision API not yet implemented. Implement in batch.");
    }

    public Task<List<DetectedLabel>> DetectLabelsAsync(
        byte[] imageData, int maxResults, CancellationToken ct)
    {
        // TODO: Implement with client.DetectLabelsAsync(image, ct, maxResults)
        _logger.LogWarning("GoogleVisionProvider.DetectLabelsAsync called but not yet implemented");
        throw new NotImplementedException("Google Vision API not yet implemented. Implement in batch.");
    }
}
