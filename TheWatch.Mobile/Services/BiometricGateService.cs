using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Biometric authentication gate (fingerprint/face) before app access.
/// Uses platform-specific biometric APIs:
/// Android: AndroidX.Biometric.BiometricPrompt
/// iOS: LocalAuthentication.LAContext
/// Windows: Windows Hello
/// Configured via user preference toggle on ProfilePage.
/// </summary>
public class BiometricGateService
{
    private readonly ILogger<BiometricGateService> _logger;

    public bool IsEnabled
    {
        get => Preferences.Get("biometric_gate", false);
        set => Preferences.Set("biometric_gate", value);
    }

    public BiometricGateService(ILogger<BiometricGateService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if biometric authentication is available on this device.
    /// </summary>
    public bool IsAvailable()
    {
        // MAUI doesn't have a built-in biometric check;
        // platform-specific code handles actual availability
        return DeviceInfo.Current.Platform == DevicePlatform.Android
            || DeviceInfo.Current.Platform == DevicePlatform.iOS
            || DeviceInfo.Current.Platform == DevicePlatform.WinUI;
    }

    /// <summary>
    /// Prompt the user for biometric authentication.
    /// Returns true if authenticated, false if failed or cancelled.
    /// </summary>
    public async Task<bool> AuthenticateAsync(string reason = "Verify your identity to access TheWatch")
    {
        if (!IsEnabled)
            return true; // Gate not enabled, pass through

        try
        {
#if ANDROID
            return await AuthenticateAndroidAsync(reason);
#elif IOS
            return await AuthenticateIosAsync(reason);
#elif WINDOWS
            return await AuthenticateWindowsAsync(reason);
#else
            return true;
#endif
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Biometric authentication failed, allowing access");
            return true; // Fail open — don't lock user out on biometric error
        }
    }

#if ANDROID
    private Task<bool> AuthenticateAndroidAsync(string reason)
    {
        var tcs = new TaskCompletionSource<bool>();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var activity = Platform.CurrentActivity;
                if (activity is null)
                {
                    tcs.SetResult(true);
                    return;
                }

                var executor = AndroidX.Core.Content.ContextCompat.GetMainExecutor(activity);
                var callback = new BiometricCallback(tcs);

                var promptInfo = new AndroidX.Biometric.BiometricPrompt.PromptInfo.Builder()
                    .SetTitle("TheWatch Authentication")
                    .SetSubtitle(reason)
                    .SetNegativeButtonText("Cancel")
                    .SetAllowedAuthenticators(AndroidX.Biometric.BiometricManager.Authenticators.BiometricStrong)
                    .Build();

                var biometricPrompt = new AndroidX.Biometric.BiometricPrompt(
                    (AndroidX.Fragment.App.FragmentActivity)activity, executor, callback);

                biometricPrompt.Authenticate(promptInfo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Android biometric prompt failed");
                tcs.TrySetResult(true);
            }
        });

        return tcs.Task;
    }

    private class BiometricCallback : AndroidX.Biometric.BiometricPrompt.AuthenticationCallback
    {
        private readonly TaskCompletionSource<bool> _tcs;

        public BiometricCallback(TaskCompletionSource<bool> tcs) => _tcs = tcs;

        public override void OnAuthenticationSucceeded(AndroidX.Biometric.BiometricPrompt.AuthenticationResult result)
            => _tcs.TrySetResult(true);

        public override void OnAuthenticationFailed()
            => _tcs.TrySetResult(false);

        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence? errString)
            => _tcs.TrySetResult(false);
    }
#endif

#if IOS
    private async Task<bool> AuthenticateIosAsync(string reason)
    {
        var context = new LocalAuthentication.LAContext();
        var (canEvaluate, _) = context.CanEvaluatePolicy(
            LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _);

        if (!canEvaluate)
        {
            _logger.LogWarning("iOS biometric not available");
            return true;
        }

        var (success, error) = await context.EvaluatePolicyAsync(
            LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics, reason);

        return success;
    }
#endif

#if WINDOWS
    private async Task<bool> AuthenticateWindowsAsync(string reason)
    {
        try
        {
            var available = await Windows.Security.Credentials.UI.UserConsentVerifier
                .CheckAvailabilityAsync();

            if (available != Windows.Security.Credentials.UI.UserConsentVerifierAvailability.Available)
                return true;

            var result = await Windows.Security.Credentials.UI.UserConsentVerifier
                .RequestVerificationAsync(reason);

            return result == Windows.Security.Credentials.UI.UserConsentVerificationResult.Verified;
        }
        catch
        {
            return true;
        }
    }
#endif
}
