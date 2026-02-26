using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Gcp;

/// <summary>
/// Google Cloud Speech-to-Text implementation of ISpeechToTextProvider (Item 132).
///
/// STUB — implement in batch. Wire up:
///   - Google.Cloud.Speech.V2.SpeechClient
///   - Recognizer with language code + enhanced model
///   - Streaming recognize for real-time transcription
///   - Word-level timing extraction
///
/// NuGet: Google.Cloud.Speech.V2
/// Docs: https://cloud.google.com/speech-to-text/v2/docs
/// </summary>
public class GoogleSpeechToTextProvider : ISpeechToTextProvider
{
    private readonly GcpServiceOptions _options;
    private readonly ILogger<GoogleSpeechToTextProvider> _logger;

    public GoogleSpeechToTextProvider(GcpServiceOptions options, ILogger<GoogleSpeechToTextProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured => true;

    public Task<SpeechTranscriptionResult> TranscribeAsync(
        byte[] audioData, AudioEncoding encoding, int sampleRateHertz, CancellationToken ct)
    {
        // TODO: Implement with Google.Cloud.Speech.V2.SpeechClient
        //
        // var client = await SpeechClient.CreateAsync(ct);
        // var request = new RecognizeRequest
        // {
        //     Recognizer = $"projects/{projectId}/locations/global/recognizers/_",
        //     Config = new RecognitionConfig
        //     {
        //         AutoDecodingConfig = new AutoDetectDecodingConfig(),
        //         LanguageCodes = { _options.SpeechLanguageCode },
        //         Features = new RecognitionFeatures
        //         {
        //             EnableAutomaticPunctuation = _options.SpeechEnablePunctuation,
        //             EnableWordTimeOffsets = true,
        //         }
        //     },
        //     Content = ByteString.CopyFrom(audioData)
        // };
        // var response = await client.RecognizeAsync(request, ct);

        _logger.LogWarning("GoogleSpeechToTextProvider.TranscribeAsync called but not yet implemented");
        throw new NotImplementedException("Google Speech-to-Text not yet implemented. Implement in batch.");
    }

    public Task<ISpeechStreamingSession> StartStreamingAsync(CancellationToken ct)
    {
        // TODO: Implement with SpeechClient.StreamingRecognize()
        _logger.LogWarning("GoogleSpeechToTextProvider.StartStreamingAsync called but not yet implemented");
        throw new NotImplementedException("Google Speech-to-Text streaming not yet implemented. Implement in batch.");
    }
}
