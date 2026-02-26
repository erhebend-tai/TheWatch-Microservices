namespace TheWatch.Mobile.Services;

/// <summary>
/// Perpetual speech-to-text listener that monitors for activation phrases.
/// Uses platform-native speech recognition APIs.
/// Currently provides the listening framework — platform speech recognition
/// implementations will be wired per-platform (Android SpeechRecognizer,
/// iOS SFSpeechRecognizer, Windows SpeechRecognition).
/// </summary>
public class SpeechListenerService : IDisposable
{
    private readonly PhraseService _phraseService;
    private CancellationTokenSource? _cts;
    private bool _isListening;

    public bool IsListening => _isListening;
    public event Action<string>? OnSpeechRecognized;
    public event Action<string, PhraseAction>? OnPhraseMatched;

    public SpeechListenerService(PhraseService phraseService)
    {
        _phraseService = phraseService;
    }

    public async Task StartListeningAsync()
    {
        if (_isListening) return;

        var status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
            return;

        _isListening = true;
        _cts = new CancellationTokenSource();
        _ = ListenLoopAsync(_cts.Token);
    }

    public Task StopListeningAsync()
    {
        _isListening = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Process recognized speech text (called by platform-specific recognizer or test harness).
    /// </summary>
    public void ProcessTranscript(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript)) return;

        OnSpeechRecognized?.Invoke(transcript);

        var match = _phraseService.MatchPhrase(transcript);
        if (match is not null)
            OnPhraseMatched?.Invoke(match.Value.phrase, match.Value.action);
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        // Platform speech recognition loop.
        // On Android: SpeechRecognizer with RECOGNIZE_SPEECH intent
        // On iOS: SFSpeechRecognizer with continuous recognition
        // On Windows: Windows.Media.SpeechRecognition.SpeechRecognizer
        //
        // For now, this loop awaits platform implementations.
        // The ProcessTranscript method is the entry point for recognized text.
        while (!ct.IsCancellationRequested && _isListening)
        {
            try
            {
                // Polling interval — platform implementations will replace this
                // with event-driven recognition callbacks
                await Task.Delay(2000, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
