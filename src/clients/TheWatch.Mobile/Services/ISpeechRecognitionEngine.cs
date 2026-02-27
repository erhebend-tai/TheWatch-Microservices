namespace TheWatch.Mobile.Services;

/// <summary>
/// Platform-specific speech recognition engine interface.
/// Implementations handle continuous speech recognition with callbacks.
/// </summary>
public interface ISpeechRecognitionEngine : IDisposable
{
    /// <summary>
    /// Start continuous speech recognition.
    /// </summary>
    Task<bool> StartAsync();

    /// <summary>
    /// Stop speech recognition.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Fired when speech is recognized (partial or final results).
    /// </summary>
    event Action<string>? OnResult;

    /// <summary>
    /// Fired when an error occurs during recognition.
    /// </summary>
    event Action<string>? OnError;

    /// <summary>
    /// Whether the engine is currently listening.
    /// </summary>
    bool IsListening { get; }
}