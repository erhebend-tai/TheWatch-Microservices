using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Helpers;

/// <summary>
/// Unified speech, microphone, and location permission handling across platforms.
/// TheWatch depends on active voice tracking for safety and security — permissions
/// are requested with rationale explaining this dependency.
/// Android: RECORD_AUDIO + ACCESS_BACKGROUND_LOCATION manifest permissions
/// iOS: SFSpeechRecognizer.RequestAuthorization() + Microphone + Location Always
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
                await ShowMicrophoneRationaleAsync();
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

    /// <summary>
    /// Request Location Always permission required for continuous safety monitoring.
    /// TheWatch depends on active voice tracking for safety and security, which
    /// requires persistent location access even when the app is backgrounded or
    /// the device is locked.
    /// </summary>
    public static async Task<bool> RequestLocationAlwaysPermissionAsync(ILogger? logger = null)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();

        if (status != PermissionStatus.Granted)
        {
            if (Permissions.ShouldShowRationale<Permissions.LocationAlways>())
            {
                await ShowLocationRationaleAsync();
            }

            status = await Permissions.RequestAsync<Permissions.LocationAlways>();
        }

        if (status != PermissionStatus.Granted)
        {
            logger?.LogWarning("Location Always permission denied");
            return false;
        }

        logger?.LogInformation("Location Always permission granted");
        return true;
    }

    /// <summary>
    /// Request all permissions needed for full TheWatch operation: microphone,
    /// speech recognition, and location always. TheWatch depends on active voice
    /// tracking for safety and security — all permissions are essential for
    /// continuous monitoring including from the lockscreen.
    /// </summary>
    public static async Task<bool> RequestAllPermissionsAsync(ILogger? logger = null)
    {
        var speechGranted = await RequestSpeechPermissionsAsync(logger);
        var locationGranted = await RequestLocationAlwaysPermissionAsync(logger);

        if (speechGranted && locationGranted)
        {
            logger?.LogInformation("All permissions granted for active voice tracking and location monitoring");
        }
        else
        {
            logger?.LogWarning("Partial permissions: speech={SpeechGranted}, location={LocationGranted}",
                speechGranted, locationGranted);
        }

        return speechGranted && locationGranted;
    }

    private static async Task ShowMicrophoneRationaleAsync()
    {
        if (Application.Current?.MainPage is not null)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Microphone Permission Required",
                "TheWatch depends on active voice tracking for safety and security. " +
                "Microphone access is needed to listen for emergency activation phrases, " +
                "enabling hands-free incident reporting even from the lockscreen.",
                "OK");
        }
    }

    private static async Task ShowLocationRationaleAsync()
    {
        if (Application.Current?.MainPage is not null)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Location Permission Required",
                "TheWatch depends on active voice tracking for safety and security. " +
                "Continuous location access (\"Always\") is required to geo-tag incidents " +
                "and enable real-time responder positioning during emergencies, " +
                "including when the app is in the background or the device is locked.",
                "OK");
        }
    }
}
