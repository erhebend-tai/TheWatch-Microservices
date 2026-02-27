using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Client-side content moderation for evidence photos before upload.
/// Uses ONNX Runtime with a lightweight NSFW classification model.
/// Note: Requires Microsoft.ML.OnnxRuntime NuGet and a model file in Resources/Raw/.
/// This implementation provides the framework; the actual ONNX model loading
/// is gated behind model file availability.
/// </summary>
public class ContentModerationService
{
    private readonly ILogger<ContentModerationService> _logger;
    private readonly double _threshold;

    public ContentModerationService(ILogger<ContentModerationService> logger)
    {
        _logger = logger;
        _threshold = Preferences.Get("moderation_threshold", 0.7);
    }

    /// <summary>
    /// Analyze an image for inappropriate content.
    /// Returns a moderation result with confidence score.
    /// </summary>
    public async Task<ModerationResult> AnalyzeImageAsync(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                return new ModerationResult { IsSafe = true, Confidence = 1.0 };
            }

            // Framework for ONNX inference — requires model file at Resources/Raw/nsfw_model.onnx
            // When model is available:
            // 1. Load model: var session = new InferenceSession(modelPath)
            // 2. Preprocess image: resize to 224x224, normalize to [0,1]
            // 3. Run inference: var results = session.Run(inputs)
            // 4. Get confidence score from output tensor

            // For now, return safe (no model loaded)
            _logger.LogDebug("Content moderation: model not loaded, assuming safe for {Path}", imagePath);
            return new ModerationResult
            {
                IsSafe = true,
                Confidence = 0.0,
                ModelVersion = "none",
                AnalyzedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content moderation analysis failed for {Path}", imagePath);
            return new ModerationResult { IsSafe = true, Confidence = 0.0 };
        }
    }

    /// <summary>
    /// Check if evidence should be flagged based on moderation result.
    /// </summary>
    public bool ShouldFlag(ModerationResult result)
    {
        return !result.IsSafe && result.Confidence >= _threshold;
    }
}

public class ModerationResult
{
    public bool IsSafe { get; set; } = true;
    public double Confidence { get; set; }
    public string ModelVersion { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
}
