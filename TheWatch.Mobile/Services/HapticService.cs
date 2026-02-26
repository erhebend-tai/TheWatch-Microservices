using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Named haptic feedback patterns for different events.
/// Uses MAUI Essentials HapticFeedback and Vibration for pattern sequences.
/// User can disable haptics via Preferences.
/// </summary>
public class HapticService
{
    private readonly ILogger<HapticService> _logger;

    public bool IsEnabled
    {
        get => Preferences.Get("haptics_enabled", true);
        set => Preferences.Set("haptics_enabled", value);
    }

    public HapticService(ILogger<HapticService> logger)
    {
        _logger = logger;
    }

    /// <summary>SOS confirmed: 3 strong pulses.</summary>
    public async Task SOSConfirmAsync()
    {
        if (!IsEnabled) return;
        await PlayPatternAsync([500, 200, 500, 200, 500]);
    }

    /// <summary>Alert received: 2 medium pulses.</summary>
    public async Task AlertReceivedAsync()
    {
        if (!IsEnabled) return;
        await PlayPatternAsync([300, 150, 300]);
    }

    /// <summary>Check-in reminder: 1 soft pulse.</summary>
    public async Task CheckInReminderAsync()
    {
        if (!IsEnabled) return;
        await PlayPatternAsync([150]);
    }

    /// <summary>Emergency alarm: rapid 5-pulse.</summary>
    public async Task EmergencyAlarmAsync()
    {
        if (!IsEnabled) return;
        await PlayPatternAsync([200, 100, 200, 100, 200, 100, 200, 100, 200]);
    }

    /// <summary>Simple click feedback.</summary>
    public void Click()
    {
        if (!IsEnabled) return;
        try
        {
            HapticFeedback.Perform(HapticFeedbackType.Click);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Haptic click failed");
        }
    }

    /// <summary>Long press feedback.</summary>
    public void LongPress()
    {
        if (!IsEnabled) return;
        try
        {
            HapticFeedback.Perform(HapticFeedbackType.LongPress);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Haptic long press failed");
        }
    }

    /// <summary>
    /// Play a vibration pattern. Array alternates: vibrate, pause, vibrate, pause...
    /// Values in milliseconds.
    /// </summary>
    private async Task PlayPatternAsync(int[] pattern)
    {
        try
        {
            foreach (var (duration, index) in pattern.Select((d, i) => (d, i)))
            {
                if (index % 2 == 0)
                {
                    // Vibrate
                    Vibration.Vibrate(TimeSpan.FromMilliseconds(duration));
                }

                // Pause (both vibrate and gap durations need a delay)
                await Task.Delay(duration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Haptic pattern playback failed");
        }
    }
}
