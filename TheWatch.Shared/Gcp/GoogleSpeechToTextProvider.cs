using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Gcp;

/// <summary>
/// Google Cloud Speech-to-Text V2 implementation of ISpeechToTextProvider (Item 132).
/// Uses the REST API via HttpClient for cross-platform compatibility.
/// </summary>
public class GoogleSpeechToTextProvider : ISpeechToTextProvider
{
    private readonly GcpServiceOptions _options;
    private readonly ILogger<GoogleSpeechToTextProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public GoogleSpeechToTextProvider(GcpServiceOptions options, ILogger<GoogleSpeechToTextProvider> logger, HttpClient? httpClient = null)
    {
        _options = options;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        _baseUrl = $"https://speech.googleapis.com/v2/projects/{options.HealthcareProjectId}/locations/global";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.CredentialPath);

    public async Task<SpeechTranscriptionResult> TranscribeAsync(
        byte[] audioData, AudioEncoding encoding, int sampleRateHertz, CancellationToken ct)
    {
        _logger.LogInformation("Transcribing {Bytes} bytes of audio (encoding={Encoding}, rate={Rate}Hz)",
            audioData.Length, encoding, sampleRateHertz);

        var request = new
        {
            config = new
            {
                autoDecodingConfig = new { },
                languageCodes = new[] { _options.SpeechLanguageCode },
                features = new
                {
                    enableAutomaticPunctuation = _options.SpeechEnablePunctuation,
                    enableWordTimeOffsets = true
                },
                model = _options.SpeechUseEnhancedModel ? "latest_long" : "latest_short"
            },
            content = Convert.ToBase64String(audioData)
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/recognizers/_:recognize", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Speech-to-Text API failed: {Status} {Error}", response.StatusCode, error);
            return new SpeechTranscriptionResult { Text = string.Empty, Confidence = 0, IsFinal = true };
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);

        var result = new SpeechTranscriptionResult();
        var results = doc.RootElement.GetProperty("results");

        if (results.GetArrayLength() > 0)
        {
            var firstResult = results[0];
            var alternatives = firstResult.GetProperty("alternatives");
            if (alternatives.GetArrayLength() > 0)
            {
                var best = alternatives[0];
                var words = new List<WordTiming>();

                if (best.TryGetProperty("words", out var wordsElement))
                {
                    foreach (var w in wordsElement.EnumerateArray())
                    {
                        var word = w.GetProperty("word").GetString() ?? "";
                        var startOffset = ParseDuration(w.GetProperty("startOffset").GetString());
                        var endOffset = ParseDuration(w.GetProperty("endOffset").GetString());
                        var conf = w.TryGetProperty("confidence", out var c) ? c.GetSingle() : 0f;
                        words.Add(new WordTiming(word, startOffset, endOffset, conf));
                    }
                }

                result = new SpeechTranscriptionResult
                {
                    Text = best.GetProperty("transcript").GetString() ?? string.Empty,
                    Confidence = best.TryGetProperty("confidence", out var confEl) ? confEl.GetSingle() : 0f,
                    IsFinal = true,
                    Words = words,
                    LanguageCode = _options.SpeechLanguageCode,
                    AudioDuration = words.Count > 0 ? words[^1].EndTime : TimeSpan.Zero
                };
            }
        }

        _logger.LogInformation("Transcription complete: {Length} chars, confidence={Confidence:F2}",
            result.Text.Length, result.Confidence);

        return result;
    }

    public async Task<ISpeechStreamingSession> StartStreamingAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting streaming Speech-to-Text session");
        var session = new SpeechStreamingSession(_httpClient, _baseUrl, _options, _logger);
        await session.InitializeAsync(ct);
        return session;
    }

    private static TimeSpan ParseDuration(string? duration)
    {
        if (string.IsNullOrEmpty(duration)) return TimeSpan.Zero;
        // Google returns durations like "1.500s"
        if (duration.EndsWith('s') && double.TryParse(duration[..^1], out var seconds))
            return TimeSpan.FromSeconds(seconds);
        return TimeSpan.Zero;
    }
}

/// <summary>
/// Streaming session that collects audio chunks and sends them for transcription.
/// Uses chunked recognize for near-real-time results.
/// </summary>
internal sealed class SpeechStreamingSession : ISpeechStreamingSession
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly GcpServiceOptions _options;
    private readonly ILogger _logger;
    private readonly List<byte> _audioBuffer = [];
    private bool _disposed;

    public event EventHandler<SpeechTranscriptionResult>? TranscriptionReceived;

    internal SpeechStreamingSession(HttpClient httpClient, string baseUrl, GcpServiceOptions options, ILogger logger)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
        _options = options;
        _logger = logger;
    }

    internal Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;

    public Task SendAudioAsync(byte[] chunk, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _audioBuffer.AddRange(chunk);

        // Flush every 32KB for interim results
        if (_audioBuffer.Count >= 32768)
            return FlushBufferAsync(isFinal: false, ct);

        return Task.CompletedTask;
    }

    public async Task CompleteAsync(CancellationToken ct = default)
    {
        if (_audioBuffer.Count > 0)
            await FlushBufferAsync(isFinal: true, ct);
    }

    private async Task FlushBufferAsync(bool isFinal, CancellationToken ct)
    {
        var audioData = _audioBuffer.ToArray();
        _audioBuffer.Clear();

        var request = new
        {
            config = new
            {
                autoDecodingConfig = new { },
                languageCodes = new[] { _options.SpeechLanguageCode },
                features = new
                {
                    enableAutomaticPunctuation = _options.SpeechEnablePunctuation,
                    enableWordTimeOffsets = true
                }
            },
            content = Convert.ToBase64String(audioData)
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/recognizers/_:recognize", content, ct);

        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var results = doc.RootElement.GetProperty("results");

            if (results.GetArrayLength() > 0)
            {
                var firstResult = results[0];
                var alternatives = firstResult.GetProperty("alternatives");
                if (alternatives.GetArrayLength() > 0)
                {
                    var best = alternatives[0];
                    var result = new SpeechTranscriptionResult
                    {
                        Text = best.GetProperty("transcript").GetString() ?? string.Empty,
                        Confidence = best.TryGetProperty("confidence", out var c) ? c.GetSingle() : 0f,
                        IsFinal = isFinal,
                        LanguageCode = _options.SpeechLanguageCode
                    };
                    TranscriptionReceived?.Invoke(this, result);
                }
            }
        }
        else
        {
            _logger.LogWarning("Streaming recognize chunk failed: {Status}", response.StatusCode);
        }
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        _audioBuffer.Clear();
        return ValueTask.CompletedTask;
    }
}
