#if IOS
using BackgroundTasks;
using Foundation;
using UIKit;

namespace TheWatch.Mobile.Platforms.iOS.Services;

/// <summary>
/// iOS background task for periodic speech recognition wake-up.
/// Uses BGProcessingTaskRequest for longer background execution time.
/// iOS limits continuous background audio, so this uses periodic wake-ups
/// combined with UIBackgroundModes audio for background audio sessions.
/// </summary>
public static class IosSpeechBackgroundTask
{
    public const string TaskIdentifier = "com.relentless.thewatch.speechlistener";

    /// <summary>
    /// Register the background task with iOS. Call from AppDelegate.FinishedLaunching.
    /// </summary>
    public static void Register()
    {
        BGTaskScheduler.Shared.Register(TaskIdentifier, null, task =>
        {
            HandleBackgroundTask((BGProcessingTask)task);
        });
    }

    /// <summary>
    /// Schedule the next background wake-up.
    /// </summary>
    public static void ScheduleNext()
    {
        var request = new BGProcessingTaskRequest(TaskIdentifier)
        {
            RequiresNetworkConnectivity = false,
            RequiresExternalPower = false,
            EarliestBeginDate = NSDate.FromTimeIntervalSinceNow(15 * 60) // 15 minutes
        };

        try
        {
            BGTaskScheduler.Shared.Submit(request, out var error);
            if (error is not null)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to schedule background task: {error}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Background task scheduling error: {ex.Message}");
        }
    }

    private static void HandleBackgroundTask(BGProcessingTask task)
    {
        // Schedule the next occurrence
        ScheduleNext();

        // Set up expiration handler
        task.ExpirationHandler = () =>
        {
            // Clean up when iOS reclaims the task
            task.SetTaskCompleted(false);
        };

        // Get the speech service and run a brief listening cycle
        var speechService = IPlatformApplication.Current?.Services.GetService<TheWatch.Mobile.Services.SpeechListenerService>();
        if (speechService is null)
        {
            task.SetTaskCompleted(true);
            return;
        }

        // Run a brief listening session (30 seconds max)
        Task.Run(async () =>
        {
            try
            {
                await speechService.StartListeningAsync();
                await Task.Delay(TimeSpan.FromSeconds(30));
                await speechService.StopListeningAsync();
                task.SetTaskCompleted(true);
            }
            catch
            {
                task.SetTaskCompleted(false);
            }
        });
    }
}
#endif
