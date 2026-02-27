using System.Globalization;
using TheWatch.Mobile.Services;
using Windows.Media.SpeechRecognition;
using Windows.Storage;

namespace TheWatch.Mobile.Platforms.Windows.Services;

/// <summary>
/// Windows implementation of speech recognition using Windows.Media.SpeechRecognition.
/// Provides continuous speech recognition for desktop testing scenarios.
/// </summary>
public class WindowsSpeechRecognizer : ISpeechRecognitionEngine
{
    private SpeechRecognizer? _speechRecognizer;
    private bool _isListening;
    private bool _disposed;

    public bool IsListening => _isListening;

    public event Action<string>? OnResult;
    public event Action<string>? OnError;

    public async Task<bool> StartAsync()
    {
        if (_isListening || _disposed)
            return false;

        try
        {
            // Create speech recognizer
            _speechRecognizer = new SpeechRecognizer();

            // Set up event handlers
            _speechRecognizer.ContinuousRecognitionSession.ResultGenerated += OnResultGenerated;
            _speechRecognizer.ContinuousRecognitionSession.Completed += OnRecognitionCompleted;
            _speechRecognizer.StateChanged += OnStateChanged;

            // Configure recognition constraints
            var webSearchGrammar = new SpeechRecognitionTopicConstraint(
                SpeechRecognitionScenario.WebSearch, 
                "webSearch");
            
            _speechRecognizer.Constraints.Add(webSearchGrammar);

            // Compile constraints
            var compilationResult = await _speechRecognizer.CompileConstraintsAsync();
            if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
            {
                OnError?.Invoke($"Failed to compile speech constraints: {compilationResult.Status}");
                return false;
            }

            // Configure recognition settings
            _speechRecognizer.Timeouts.InitialSilenceTimeout = TimeSpan.FromSeconds(5);
            _speechRecognizer.Timeouts.BabbleTimeout = TimeSpan.FromSeconds(2);
            _speechRecognizer.Timeouts.EndSilenceTimeout = TimeSpan.FromSeconds(1);

            // Start continuous recognition
            await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
            _isListening = true;

            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Failed to start speech recognition: {ex.Message}");
            return false;
        }
    }

    public async Task StopAsync()
    {
        if (!_isListening || _disposed)
            return;

        try
        {
            _isListening = false;

            if (_speechRecognizer?.ContinuousRecognitionSession != null)
            {
                await _speechRecognizer.ContinuousRecognitionSession.StopAsync();
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Error stopping speech recognition: {ex.Message}");
        }
    }

    private void OnResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
    {
        if (args.Result?.Status == SpeechRecognitionResultStatus.Success)
        {
            var transcript = args.Result.Text;
            if (!string.IsNullOrWhiteSpace(transcript))
            {
                OnResult?.Invoke(transcript);
            }
        }
    }

    private async void OnRecognitionCompleted(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
    {
        // Restart recognition if still listening and not disposed
        if (_isListening && !_disposed)
        {
            try
            {
                await Task.Delay(100); // Brief pause before restarting
                if (_isListening && !_disposed)
                {
                    await _speechRecognizer?.ContinuousRecognitionSession.StartAsync();
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Error restarting recognition: {ex.Message}");
            }
        }
    }

    private void OnStateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
    {
        // Handle state changes if needed
        if (args.State == SpeechRecognizerState.Idle && _isListening && !_disposed)
        {
            // Recognition went idle, try to restart
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(100);
                    if (_isListening && !_disposed)
                    {
                        await _speechRecognizer?.ContinuousRecognitionSession.StartAsync();
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"Error restarting from idle state: {ex.Message}");
                }
            });
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _isListening = false;

        try
        {
            if (_speechRecognizer != null)
            {
                // Unsubscribe from events
                _speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= OnResultGenerated;
                _speechRecognizer.ContinuousRecognitionSession.Completed -= OnRecognitionCompleted;
                _speechRecognizer.StateChanged -= OnStateChanged;

                // Stop and dispose
                _speechRecognizer.ContinuousRecognitionSession.StopAsync().AsTask().Wait(1000);
                _speechRecognizer.Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disposing WindowsSpeechRecognizer: {ex.Message}");
        }

        _speechRecognizer = null;
    }
}