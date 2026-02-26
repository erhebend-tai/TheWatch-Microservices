#if ANDROID
using Android.App;
using Firebase.Messaging;
using Microsoft.Extensions.Logging;
using TheWatch.Mobile.Services;

namespace TheWatch.Mobile.Platforms.Android;

[Service(Exported = true)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class WatchFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);

        // Resolve the push service and register the new token
        var pushService = MauiApplication.Current.Services
            .GetService<WatchPushNotificationService>();
        if (pushService != null)
        {
            _ = pushService.OnTokenRefreshedAsync(token);
        }
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        var pushService = MauiApplication.Current.Services
            .GetService<WatchPushNotificationService>();
        if (pushService == null) return;

        var data = new PushNotificationData
        {
            Title = message.GetNotification()?.Title,
            Body = message.GetNotification()?.Body,
            ImageUrl = message.GetNotification()?.ImageUrl?.ToString(),
            Data = message.Data?.ToDictionary(k => k.Key, v => v.Value) ?? []
        };

        // If the app is in the foreground, handle directly
        pushService.HandleNotificationReceived(data);

        // Also show a local notification so the user sees it
        ShowLocalNotification(data);
    }

    private void ShowLocalNotification(PushNotificationData data)
    {
        var channelId = data.Data.TryGetValue("source", out var source)
            ? $"watch_{source.ToLowerInvariant()}"
            : "watch_system";

        EnsureNotificationChannel(channelId);

        var intent = new global::Android.Content.Intent(this, typeof(MainActivity));
        intent.SetFlags(global::Android.Content.ActivityFlags.SingleTop);
        foreach (var kvp in data.Data)
            intent.PutExtra(kvp.Key, kvp.Value);

        var pendingIntent = PendingIntent.GetActivity(
            this, 0, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var notification = new Notification.Builder(this, channelId)
            .SetContentTitle(data.Title ?? "TheWatch")
            .SetContentText(data.Body ?? "")
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetAutoCancel(true)
            .SetContentIntent(pendingIntent)
            .Build();

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.Notify(DateTime.UtcNow.Millisecond, notification);
    }

    private void EnsureNotificationChannel(string channelId)
    {
        if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.O)
            return;

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        if (manager?.GetNotificationChannel(channelId) != null)
            return;

        var importance = channelId.Contains("emergency") || channelId.Contains("dispatch")
            ? NotificationImportance.High
            : NotificationImportance.Default;

        var channel = new NotificationChannel(channelId, channelId, importance)
        {
            Description = $"TheWatch {channelId} notifications"
        };

        if (importance == NotificationImportance.High)
        {
            channel.EnableVibration(true);
            channel.EnableLights(true);
        }

        manager?.CreateNotificationChannel(channel);
    }
}
#endif
