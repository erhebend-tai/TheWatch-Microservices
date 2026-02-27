namespace TheWatch.Shared.Gcp;

/// <summary>
/// Server-side speech-to-text provider interface (Item 132).
/// Used by P2 VoiceEmergency for processing recorded audio from incidents.
///
/// Implementations:
///   - NoOpSpeechToTextProvider: development/testing (returns empty results)
///   - GoogleSpeechToTextProvider: Google Cloud Speech-to-Text API (implement in batch)
///
/// Toggle via Gcp:UseSpeechToText = true in appsettings.json.
/// </summary>
public interface ISpeechToTextProvider
{
    /// <summary>
    /// Transcribe audio data to text.
    /// </summary>
    /// <param name="audioData">Raw audio bytes (PCM, FLAC, or OGG).</param>
    /// <param name="encoding">Audio encoding format.</param>
    /// <param name="sampleRateHertz">Sample rate in Hz (e.g., 16000).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Transcription result with text, confidence, and word-level timing.</returns>
    Task<SpeechTranscriptionResult> TranscribeAsync(
        byte[] audioData,
        AudioEncoding encoding = AudioEncoding.Linear16,
        int sampleRateHertz = 16000,
        CancellationToken ct = default);

    /// <summary>
    /// Start a streaming transcription session for real-time audio.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Streaming session handle.</returns>
    Task<ISpeechStreamingSession> StartStreamingAsync(CancellationToken ct = default);

    /// <summary>
    /// Whether the provider is configured and ready.
    /// </summary>
    bool IsConfigured { get; }
}

/// <summary>
/// Streaming speech-to-text session for real-time audio.
/// </summary>
public interface ISpeechStreamingSession : IAsyncDisposable
{
    /// <summary>
    /// Send an audio chunk for real-time transcription.
    /// </summary>
    Task SendAudioAsync(byte[] chunk, CancellationToken ct = default);

    /// <summary>
    /// Signal end of audio input.
    /// </summary>
    Task CompleteAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when a partial or final transcription is available.
    /// </summary>
    event EventHandler<SpeechTranscriptionResult>? TranscriptionReceived;
}

// ─── DTOs ───

public record SpeechTranscriptionResult
{
    public string Text { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public bool IsFinal { get; init; }
    public TimeSpan AudioDuration { get; init; }
    public List<WordTiming> Words { get; init; } = [];
    public string LanguageCode { get; init; } = "en-US";
}

public record WordTiming(string Word, TimeSpan StartTime, TimeSpan EndTime, float Confidence);

public enum AudioEncoding
{
    Linear16,
    Flac,
    OggOpus,
    Mulaw,
    Mp3
}
