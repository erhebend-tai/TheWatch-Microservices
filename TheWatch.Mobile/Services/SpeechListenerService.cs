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
    private readonly BatteryMonitorService _batteryMonitor;
    private CancellationTokenSource? _cts;
    private bool _isListening;

    public bool IsListening => _isListening;
    public event Action<string>? OnSpeechRecognized;
    public event Action<string, PhraseAction>? OnPhraseMatched;

    public SpeechListenerService(PhraseService phraseService, BatteryMonitorService batteryMonitor)
    {
        _phraseService = phraseService;
        _batteryMonitor = batteryMonitor;
    }

    public async Task StartListeningAsync()
    {
        if (_isListening) return;

        // Request all required permissions using the unified helper
        var permissionsGranted = await Helpers.PermissionHelper.RequestSpeechPermissionsAsync();
        if (!permissionsGranted)
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
        // Get platform-specific speech recognizer
        var speechEngine = GetPlatformSpeechEngine();
        if (speechEngine == null)
        {
            return;
        }

        // Wire up events
        speechEngine.OnResult += ProcessTranscript;
        speechEngine.OnError += (error) => 
        {
            // Log error and attempt restart
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000, ct);
                if (_isListening && !ct.IsCancellationRequested)
                {
                    await speechEngine.StartAsync();
                }
            });
        };

        // Start platform recognition
        var started = await speechEngine.StartAsync();
        if (!started)
        {
            return;
        }

        try
        {
            // Keep the service alive while listening
            while (!ct.IsCancellationRequested && _isListening)
            {
                await Task.Delay(1000, ct);
            }
        }
        finally
        {
            await speechEngine.StopAsync();
            speechEngine.OnResult -= ProcessTranscript;
            speechEngine.Dispose();
        }
    }

    private ISpeechRecognitionEngine? GetPlatformSpeechEngine()
    {
#if ANDROID
        return new Platforms.Android.Services.AndroidSpeechRecognizer();
#elif IOS
        return new Platforms.iOS.Services.IosSpeechRecognizer();
#elif WINDOWS
        return new Platforms.Windows.Services.WindowsSpeechRecognizer();
#else
        return null;
#endif
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
