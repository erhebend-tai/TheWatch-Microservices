using Android.Content;
using Android.OS;
using Android.Speech;
using AndroidX.Core.Content;
using Java.Util;
using TheWatch.Mobile.Services;

namespace TheWatch.Mobile.Platforms.Android.Services;

/// <summary>
/// Android implementation of speech recognition using Android.Speech.SpeechRecognizer.
/// Provides continuous speech recognition with partial results for emergency phrase detection.
/// </summary>
public class AndroidSpeechRecognizer : ISpeechRecognitionEngine
{
    private SpeechRecognizer? _speechRecognizer;
    private Intent? _speechRecognizerIntent;
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
            var context = Platform.CurrentActivity ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (context == null)
            {
                OnError?.Invoke("No current activity available for speech recognition");
                return false;
            }

            // Check if speech recognition is available
            if (!SpeechRecognizer.IsRecognitionAvailable(context))
            {
                OnError?.Invoke("Speech recognition not available on this device");
                return false;
            }

            // Create speech recognizer
            _speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(context);
            if (_speechRecognizer == null)
            {
                OnError?.Invoke("Failed to create speech recognizer");
                return false;
            }

            // Set up recognition listener
            _speechRecognizer.SetRecognitionListener(new AndroidRecognitionListener(this));

            // Create intent for continuous recognition
            _speechRecognizerIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            _speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            _speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.Default);
            _speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraPartialResults, true);
            _speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
            _speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 2000);
            _speechRecognizerIntent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 2000);

            // Start listening
            _speechRecognizer.StartListening(_speechRecognizerIntent);
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
            _speechRecognizer?.StopListening();
            _isListening = false;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Error stopping speech recognition: {ex.Message}");
        }
    }

    internal void HandleResult(string result)
    {
        if (!string.IsNullOrWhiteSpace(result))
        {
            OnResult?.Invoke(result);
        }
    }

    internal void HandleError(string error)
    {
        _isListening = false;
        OnError?.Invoke(error);
    }

    internal void HandleEndOfSpeech()
    {
        // Restart listening for continuous recognition
        if (_isListening && !_disposed)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // Brief pause before restarting
                if (_isListening && !_disposed)
                {
                    _speechRecognizer?.StartListening(_speechRecognizerIntent);
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
            _speechRecognizer?.Destroy();
            _speechRecognizer?.Dispose();
            _speechRecognizerIntent?.Dispose();
        }
        catch (Exception ex)
        {
            // Log but don't throw during disposal
            System.Diagnostics.Debug.WriteLine($"Error disposing AndroidSpeechRecognizer: {ex.Message}");
        }

        _speechRecognizer = null;
        _speechRecognizerIntent = null;
    }
}

/// <summary>
/// Recognition listener implementation for handling Android speech recognition callbacks.
/// </summary>
internal class AndroidRecognitionListener : Java.Lang.Object, IRecognitionListener
{
    private readonly AndroidSpeechRecognizer _parent;

    public AndroidRecognitionListener(AndroidSpeechRecognizer parent)
    {
        _parent = parent;
    }

    public void OnBeginningOfSpeech()
    {
        // Speech input has begun
    }

    public void OnBufferReceived(byte[]? buffer)
    {
        // Audio buffer received - not used for our implementation
    }

    public void OnEndOfSpeech()
    {
        _parent.HandleEndOfSpeech();
    }

    public void OnError(SpeechRecognizerError error)
    {
        var errorMessage = error switch
        {
            SpeechRecognizerError.Audio => "Audio recording error",
            SpeechRecognizerError.Client => "Client side error",
            SpeechRecognizerError.InsufficientPermissions => "Insufficient permissions",
            SpeechRecognizerError.Network => "Network error",
            SpeechRecognizerError.NetworkTimeout => "Network timeout",
            SpeechRecognizerError.NoMatch => "No speech match",
            SpeechRecognizerError.RecognizerBusy => "Recognition service busy",
            SpeechRecognizerError.Server => "Server error",
            SpeechRecognizerError.SpeechTimeout => "No speech input",
            _ => $"Unknown error: {error}"
        };

        // Don't treat "no match" or "speech timeout" as hard errors - just restart
        if (error == SpeechRecognizerError.NoMatch || error == SpeechRecognizerError.SpeechTimeout)
        {
            _parent.HandleEndOfSpeech(); // Restart listening
        }
        else
        {
            _parent.HandleError(errorMessage);
        }
    }

    public void OnEvent(int eventType, Bundle? @params)
    {
        // Additional events - not used for our implementation
    }

    public void OnPartialResults(Bundle? partialResults)
    {
        if (partialResults?.GetStringArrayList(SpeechRecognizer.ResultsRecognition) is { } results && results.Count > 0)
        {
            var result = results[0]?.ToString();
            if (!string.IsNullOrWhiteSpace(result))
            {
                _parent.HandleResult(result);
            }
        }
    }

    public void OnReadyForSpeech(Bundle? @params)
    {
        // Ready to receive speech input
    }

    public void OnResults(Bundle? results)
    {
        if (results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition) is { } recognitionResults && recognitionResults.Count > 0)
        {
            var result = recognitionResults[0]?.ToString();
            if (!string.IsNullOrWhiteSpace(result))
            {
                _parent.HandleResult(result);
            }
        }
    }

    public void OnRmsChanged(float rmsdB)
    {
        // RMS value changed - could be used for audio level indication
    }
}