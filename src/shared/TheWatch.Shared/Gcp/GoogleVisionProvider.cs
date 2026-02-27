using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Gcp;

/// <summary>
/// Google Cloud Vision API implementation of IContentAnalysisProvider (Item 133).
/// Uses the REST API for SafeSearch, label detection, and OCR.
/// </summary>
public class GoogleVisionProvider : IContentAnalysisProvider
{
    private const string VisionApiUrl = "https://vision.googleapis.com/v1/images:annotate";

    private readonly GcpServiceOptions _options;
    private readonly ILogger<GoogleVisionProvider> _logger;
    private readonly HttpClient _httpClient;

    public GoogleVisionProvider(GcpServiceOptions options, ILogger<GoogleVisionProvider> logger, HttpClient? httpClient = null)
    {
        _options = options;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.CredentialPath);

    public async Task<ContentAnalysisResult> AnalyzeImageAsync(
        byte[] imageData, string mimeType, CancellationToken ct)
    {
        _logger.LogInformation("Analyzing image ({Bytes} bytes, {MimeType}) for content moderation",
            imageData.Length, mimeType);

        var features = new List<object>();
        if (_options.VisionEnableSafeSearch)
            features.Add(new { type = "SAFE_SEARCH_DETECTION" });
        if (_options.VisionEnableLabelDetection)
            features.Add(new { type = "LABEL_DETECTION", maxResults = 10 });

        var request = new
        {
            requests = new[]
            {
                new
                {
                    image = new { content = Convert.ToBase64String(imageData) },
                    features
                }
            }
        };

        var response = await SendVisionRequestAsync(request, ct);
        return ParseAnalysisResponse(response);
    }

    public async Task<ContentAnalysisResult> AnalyzeImageUrlAsync(string imageUrl, CancellationToken ct)
    {
        _logger.LogInformation("Analyzing image URL: {Url}", imageUrl);

        var features = new List<object>();
        if (_options.VisionEnableSafeSearch)
            features.Add(new { type = "SAFE_SEARCH_DETECTION" });
        if (_options.VisionEnableLabelDetection)
            features.Add(new { type = "LABEL_DETECTION", maxResults = 10 });

        var request = new
        {
            requests = new[]
            {
                new
                {
                    image = new { source = new { imageUri = imageUrl } },
                    features
                }
            }
        };

        var response = await SendVisionRequestAsync(request, ct);
        return ParseAnalysisResponse(response);
    }

    public async Task<TextExtractionResult> ExtractTextAsync(
        byte[] imageData, string mimeType, CancellationToken ct)
    {
        _logger.LogInformation("Extracting text from image ({Bytes} bytes)", imageData.Length);

        var request = new
        {
            requests = new[]
            {
                new
                {
                    image = new { content = Convert.ToBase64String(imageData) },
                    features = new object[]
                    {
                        new { type = "TEXT_DETECTION" }
                    }
                }
            }
        };

        var responseJson = await SendVisionRequestAsync(request, ct);
        return ParseTextExtractionResponse(responseJson);
    }

    public async Task<List<DetectedLabel>> DetectLabelsAsync(
        byte[] imageData, int maxResults, CancellationToken ct)
    {
        _logger.LogInformation("Detecting labels in image ({Bytes} bytes, max={Max})", imageData.Length, maxResults);

        var request = new
        {
            requests = new[]
            {
                new
                {
                    image = new { content = Convert.ToBase64String(imageData) },
                    features = new object[]
                    {
                        new { type = "LABEL_DETECTION", maxResults }
                    }
                }
            }
        };

        var responseJson = await SendVisionRequestAsync(request, ct);
        return ParseLabelsResponse(responseJson);
    }

    private async Task<string> SendVisionRequestAsync(object request, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(VisionApiUrl, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Vision API failed: {Status} {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Vision API returned {response.StatusCode}: {error}");
        }

        return await response.Content.ReadAsStringAsync(ct);
    }

    private ContentAnalysisResult ParseAnalysisResponse(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var responses = doc.RootElement.GetProperty("responses");

        if (responses.GetArrayLength() == 0)
            return new ContentAnalysisResult { RawResponse = responseJson };

        var first = responses[0];
        var result = new ContentAnalysisResult { RawResponse = responseJson };
        var flags = new List<ModerationFlag>();
        var labels = new List<DetectedLabel>();
        var safeSearch = new SafeSearchResult();

        // Parse SafeSearch
        if (first.TryGetProperty("safeSearchAnnotation", out var ss))
        {
            safeSearch = new SafeSearchResult
            {
                Adult = ParseLikelihood(ss, "adult"),
                Violence = ParseLikelihood(ss, "violence"),
                Racy = ParseLikelihood(ss, "racy"),
                Medical = ParseLikelihood(ss, "medical"),
                Spoof = ParseLikelihood(ss, "spoof")
            };

            // Flag anything Possible or higher
            if (safeSearch.Adult >= Likelihood.Possible)
                flags.Add(new ModerationFlag("adult", (float)safeSearch.Adult / 5, "Adult content detected"));
            if (safeSearch.Violence >= Likelihood.Possible)
                flags.Add(new ModerationFlag("violence", (float)safeSearch.Violence / 5, "Violent content detected"));
            if (safeSearch.Racy >= Likelihood.Likely)
                flags.Add(new ModerationFlag("racy", (float)safeSearch.Racy / 5, "Racy content detected"));
        }

        // Parse Labels
        if (first.TryGetProperty("labelAnnotations", out var la))
        {
            foreach (var label in la.EnumerateArray())
            {
                var name = label.GetProperty("description").GetString() ?? "";
                var score = label.TryGetProperty("score", out var s) ? s.GetSingle() : 0f;
                var category = label.TryGetProperty("topicality", out var t) ? $"topicality:{t.GetSingle():F2}" : null;
                labels.Add(new DetectedLabel(name, score, category));
            }
        }

        var isSafe = safeSearch.Adult < Likelihood.Possible &&
                     safeSearch.Violence < Likelihood.Possible &&
                     safeSearch.Racy < Likelihood.Likely;

        return result with
        {
            IsSafe = isSafe,
            SafeSearch = safeSearch,
            Labels = labels,
            Flags = flags
        };
    }

    private TextExtractionResult ParseTextExtractionResponse(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var responses = doc.RootElement.GetProperty("responses");

        if (responses.GetArrayLength() == 0)
            return new TextExtractionResult();

        var first = responses[0];
        var blocks = new List<TextBlock>();
        var fullText = string.Empty;
        var language = string.Empty;

        if (first.TryGetProperty("fullTextAnnotation", out var fta))
        {
            fullText = fta.GetProperty("text").GetString() ?? "";
            if (fta.TryGetProperty("pages", out var pages) && pages.GetArrayLength() > 0)
            {
                var page = pages[0];
                if (page.TryGetProperty("property", out var prop) &&
                    prop.TryGetProperty("detectedLanguages", out var langs) &&
                    langs.GetArrayLength() > 0)
                {
                    language = langs[0].GetProperty("languageCode").GetString() ?? "";
                }
            }
        }
        else if (first.TryGetProperty("textAnnotations", out var ta) && ta.GetArrayLength() > 0)
        {
            fullText = ta[0].GetProperty("description").GetString() ?? "";
            if (ta[0].TryGetProperty("locale", out var loc))
                language = loc.GetString() ?? "";

            // Individual text blocks (skip first which is full text)
            for (int i = 1; i < ta.GetArrayLength(); i++)
            {
                var text = ta[i].GetProperty("description").GetString() ?? "";
                blocks.Add(new TextBlock(text, 1.0f));
            }
        }

        return new TextExtractionResult
        {
            FullText = fullText,
            Blocks = blocks,
            DetectedLanguage = language
        };
    }

    private List<DetectedLabel> ParseLabelsResponse(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var responses = doc.RootElement.GetProperty("responses");
        var labels = new List<DetectedLabel>();

        if (responses.GetArrayLength() > 0)
        {
            var first = responses[0];
            if (first.TryGetProperty("labelAnnotations", out var la))
            {
                foreach (var label in la.EnumerateArray())
                {
                    var name = label.GetProperty("description").GetString() ?? "";
                    var score = label.TryGetProperty("score", out var s) ? s.GetSingle() : 0f;
                    labels.Add(new DetectedLabel(name, score));
                }
            }
        }

        return labels;
    }

    private static Likelihood ParseLikelihood(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return Likelihood.Unknown;

        return value.GetString() switch
        {
            "VERY_UNLIKELY" => Likelihood.VeryUnlikely,
            "UNLIKELY" => Likelihood.Unlikely,
            "POSSIBLE" => Likelihood.Possible,
            "LIKELY" => Likelihood.Likely,
            "VERY_LIKELY" => Likelihood.VeryLikely,
            _ => Likelihood.Unknown
        };
    }
}
