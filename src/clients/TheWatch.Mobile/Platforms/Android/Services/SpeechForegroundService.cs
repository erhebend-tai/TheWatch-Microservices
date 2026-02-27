#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace TheWatch.Mobile.Platforms.Android.Services;

/// <summary>
/// Android foreground service for perpetual speech listening.
/// Required for Android 14+ to access microphone from background.
/// Shows a persistent notification: "TheWatch is listening for emergencies".
/// </summary>
[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMicrophone)]
public class SpeechForegroundService : Service
{
    private const int NotificationId = 1001;
    private const string ChannelId = "thewatch_listening";
    private const string ChannelName = "Emergency Listening";

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        var notification = BuildNotification();
        StartForeground(NotificationId, notification);
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        StopForeground(StopForegroundFlags.Remove);
        base.OnDestroy();
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low)
        {
            Description = "Keeps TheWatch listening for emergency activation phrases"
        };
        channel.SetShowBadge(false);

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification()
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop);
        var pendingIntent = PendingIntent.GetActivity(this, 0, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("TheWatch Active")
            .SetContentText("Listening for emergency activation phrases")
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .SetContentIntent(pendingIntent)
            .SetCategory(Notification.CategoryService)
            .Build();
    }

    // Static helpers for starting/stopping from shared code
    public static void Start()
    {
        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(SpeechForegroundService));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }

    public static void Stop()
    {
        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(SpeechForegroundService));
        context.StopService(intent);
    }
}
#endif
