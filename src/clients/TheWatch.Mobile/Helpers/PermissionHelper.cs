using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Helpers;

/// <summary>
/// Unified speech and microphone permission handling across platforms.
/// Android: RECORD_AUDIO manifest permission
/// iOS: SFSpeechRecognizer.RequestAuthorization() + Microphone
/// Windows: Speech capability in manifest
/// </summary>
public static class PermissionHelper
{
    /// <summary>
    /// Request all permissions needed for speech recognition on the current platform.
    /// Shows a rationale dialog if the system suggests it.
    /// </summary>
    public static async Task<bool> RequestSpeechPermissionsAsync(ILogger? logger = null)
    {
        // Microphone is required on all platforms
        var micStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();

        if (micStatus != PermissionStatus.Granted)
        {
            if (Permissions.ShouldShowRationale<Permissions.Microphone>())
            {
                await ShowRationaleAsync();
            }

            micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
        }

        if (micStatus != PermissionStatus.Granted)
        {
            logger?.LogWarning("Microphone permission denied");
            return false;
        }

#if IOS
        // iOS also requires explicit speech recognition authorization
        var speechStatus = await Permissions.CheckStatusAsync<Permissions.Speech>();
        if (speechStatus != PermissionStatus.Granted)
        {
            speechStatus = await Permissions.RequestAsync<Permissions.Speech>();
        }

        if (speechStatus != PermissionStatus.Granted)
        {
            logger?.LogWarning("Speech recognition permission denied on iOS");
            return false;
        }
#endif

        logger?.LogInformation("Speech permissions granted");
        return true;
    }

    private static async Task ShowRationaleAsync()
    {
        // Display explanation of why perpetual listening is needed
        if (Application.Current?.MainPage is not null)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Microphone Permission Required",
                "TheWatch needs microphone access to listen for emergency activation phrases. " +
                "This enables hands-free emergency reporting when you say your configured activation phrase.",
                "OK");
        }
    }
}
