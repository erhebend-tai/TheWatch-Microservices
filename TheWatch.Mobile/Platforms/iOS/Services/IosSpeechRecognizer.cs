using AVFoundation;
using Foundation;
using Speech;
using TheWatch.Mobile.Services;

namespace TheWatch.Mobile.Platforms.iOS.Services;

/// <summary>
/// iOS implementation of speech recognition using SFSpeechRecognizer.
/// Uses on-device recognition for privacy compliance.
/// </summary>
public class IosSpeechRecognizer : ISpeechRecognitionEngine
{
    private SFSpeechRecognizer? _speechRecognizer;
    private AVAudioEngine? _audioEngine;
    private SFSpeechAudioBufferRecognitionRequest? _recognitionRequest;
    private SFSpeechRecognitionTask? _recognitionTask;
    private AVAudioInputNode? _inputNode;
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
            // Request speech recognition authorization
            var authStatus = await RequestSpeechAuthorizationAsync();
            if (authStatus != SFSpeechRecognizerAuthorizationStatus.Authorized)
            {
                OnError?.Invoke($"Speech recognition not authorized: {authStatus}");
                return false;
            }

            // Create speech recognizer for current locale
            _speechRecognizer = new SFSpeechRecognizer();
            if (_speechRecognizer == null || !_speechRecognizer.Available)
            {
                OnError?.Invoke("Speech recognizer not available");
                return false;
            }

            // Configure for on-device recognition (privacy requirement)
            if (_speechRecognizer.RespondsToSelector(new ObjCRuntime.Selector("supportsOnDeviceRecognition")))
            {
                _speechRecognizer.SetValueForKey(NSNumber.FromBoolean(true), new NSString("requiresOnDeviceRecognition"));
            }

            // Set up audio engine
            _audioEngine = new AVAudioEngine();
            _inputNode = _audioEngine.InputNode;

            if (_inputNode == null)
            {
                OnError?.Invoke("Audio input node not available");
                return false;
            }

            // Create recognition request
            _recognitionRequest = new SFSpeechAudioBufferRecognitionRequest
            {
                ShouldReportPartialResults = true
            };

            // Configure for on-device recognition if supported
            if (_recognitionRequest.RespondsToSelector(new ObjCRuntime.Selector("requiresOnDeviceRecognition")))
            {
                _recognitionRequest.RequiresOnDeviceRecognition = true;
            }

            // Set up audio format
            var recordingFormat = _inputNode.OutputFormatForBus(0);
            _inputNode.InstallTapOnBus(0, 1024, recordingFormat, (buffer, when) =>
            {
                _recognitionRequest?.Append(buffer);
            });

            // Start audio engine
            _audioEngine.Prepare();
            var audioStarted = _audioEngine.StartAndReturnError(out var audioError);
            if (!audioStarted || audioError != null)
            {
                OnError?.Invoke($"Failed to start audio engine: {audioError?.LocalizedDescription}");
                return false;
            }

            // Start recognition task
            _recognitionTask = _speechRecognizer.GetRecognitionTask(_recognitionRequest, (result, error) =>
            {
                if (error != null)
                {
                    HandleError($"Recognition error: {error.LocalizedDescription}");
                    return;
                }

                if (result != null)
                {
                    var transcript = result.BestTranscription?.FormattedString;
                    if (!string.IsNullOrWhiteSpace(transcript))
                    {
                        OnResult?.Invoke(transcript);
                    }

                    // If final result, restart for continuous recognition
                    if (result.Final && _isListening && !_disposed)
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(100);
                            await RestartRecognitionAsync();
                        });
                    }
                }
            });

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

            _recognitionTask?.Cancel();
            _recognitionTask?.Dispose();
            _recognitionTask = null;

            _audioEngine?.Stop();
            _inputNode?.RemoveTapOnBus(0);

            _recognitionRequest?.EndAudio();
            _recognitionRequest?.Dispose();
            _recognitionRequest = null;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Error stopping speech recognition: {ex.Message}");
        }
    }

    private async Task<SFSpeechRecognizerAuthorizationStatus> RequestSpeechAuthorizationAsync()
    {
        var tcs = new TaskCompletionSource<SFSpeechRecognizerAuthorizationStatus>();

        SFSpeechRecognizer.RequestAuthorization((status) =>
        {
            tcs.SetResult(status);
        });

        return await tcs.Task;
    }

    private async Task RestartRecognitionAsync()
    {
        if (!_isListening || _disposed)
            return;

        try
        {
            // Clean up current recognition
            _recognitionTask?.Cancel();
            _recognitionTask?.Dispose();
            _recognitionTask = null;

            _recognitionRequest?.EndAudio();
            _recognitionRequest?.Dispose();

            // Create new recognition request
            _recognitionRequest = new SFSpeechAudioBufferRecognitionRequest
            {
                ShouldReportPartialResults = true
            };

            if (_recognitionRequest.RespondsToSelector(new ObjCRuntime.Selector("requiresOnDeviceRecognition")))
            {
                _recognitionRequest.RequiresOnDeviceRecognition = true;
            }

            // Start new recognition task
            _recognitionTask = _speechRecognizer?.GetRecognitionTask(_recognitionRequest, (result, error) =>
            {
                if (error != null)
                {
                    HandleError($"Recognition error: {error.LocalizedDescription}");
                    return;
                }

                if (result != null)
                {
                    var transcript = result.BestTranscription?.FormattedString;
                    if (!string.IsNullOrWhiteSpace(transcript))
                    {
                        OnResult?.Invoke(transcript);
                    }

                    if (result.Final && _isListening && !_disposed)
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(100);
                            await RestartRecognitionAsync();
                        });
                    }
                }
            });
        }
        catch (Exception ex)
        {
            HandleError($"Error restarting recognition: {ex.Message}");
        }
    }

    private void HandleError(string error)
    {
        _isListening = false;
        OnError?.Invoke(error);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _isListening = false;

        try
        {
            _recognitionTask?.Cancel();
            _recognitionTask?.Dispose();

            _audioEngine?.Stop();
            _inputNode?.RemoveTapOnBus(0);
            _audioEngine?.Dispose();

            _recognitionRequest?.EndAudio();
            _recognitionRequest?.Dispose();

            _speechRecognizer?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disposing IosSpeechRecognizer: {ex.Message}");
        }

        _recognitionTask = null;
        _audioEngine = null;
        _inputNode = null;
        _recognitionRequest = null;
        _speechRecognizer = null;
    }
}