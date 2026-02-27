using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Gcp;

/// <summary>
/// No-op speech-to-text provider for development/testing.
/// Returns empty results. Replace with GoogleSpeechToTextProvider when implementing.
/// </summary>
public class NoOpSpeechToTextProvider : ISpeechToTextProvider
{
    private readonly ILogger<NoOpSpeechToTextProvider> _logger;

    public NoOpSpeechToTextProvider(ILogger<NoOpSpeechToTextProvider> logger)
    {
        _logger = logger;
    }

    public bool IsConfigured => false;

    public Task<SpeechTranscriptionResult> TranscribeAsync(
        byte[] audioData, AudioEncoding encoding, int sampleRateHertz, CancellationToken ct)
    {
        _logger.LogDebug("NoOp Speech-to-Text: TranscribeAsync called with {Bytes} bytes", audioData.Length);
        return Task.FromResult(new SpeechTranscriptionResult
        {
            Text = string.Empty,
            Confidence = 0f,
            IsFinal = true,
            AudioDuration = TimeSpan.Zero
        });
    }

    public Task<ISpeechStreamingSession> StartStreamingAsync(CancellationToken ct)
    {
        _logger.LogDebug("NoOp Speech-to-Text: StartStreamingAsync called");
        return Task.FromResult<ISpeechStreamingSession>(new NoOpStreamingSession());
    }

    private sealed class NoOpStreamingSession : ISpeechStreamingSession
    {
        public event EventHandler<SpeechTranscriptionResult>? TranscriptionReceived;
        public Task SendAudioAsync(byte[] chunk, CancellationToken ct) => Task.CompletedTask;
        public Task CompleteAsync(CancellationToken ct) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
