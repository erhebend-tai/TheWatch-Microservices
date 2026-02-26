#if IOS
using Foundation;
using UIKit;
using UserNotifications;

namespace TheWatch.Mobile.Platforms.iOS;

/// <summary>
/// iOS push notification delegate. Handles token registration and notification display.
/// Wire up in AppDelegate by calling WatchPushNotificationDelegate.Register().
/// </summary>
public class WatchPushNotificationDelegate : NSObject, IUNUserNotificationCenterDelegate
{
    private static WatchPushNotificationDelegate? _instance;

    public static void Register()
    {
        _instance = new WatchPushNotificationDelegate();

        UNUserNotificationCenter.Current.Delegate = _instance;

        // Request notification permission
        UNUserNotificationCenter.Current.RequestAuthorization(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
            (granted, error) =>
            {
                if (granted)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UIApplication.SharedApplication.RegisterForRemoteNotifications();
                    });
                }
            });
    }

    /// <summary>
    /// Called when a notification is received while the app is in the foreground.
    /// </summary>
    [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
    public void WillPresentNotification(
        UNUserNotificationCenter center,
        UNNotification notification,
        Action<UNNotificationPresentationOptions> completionHandler)
    {
        var userInfo = notification.Request.Content.UserInfo;
        var pushService = GetPushService();

        if (pushService != null)
        {
            var data = ExtractData(userInfo);
            data.Title = notification.Request.Content.Title;
            data.Body = notification.Request.Content.Body;
            pushService.HandleNotificationReceived(data);
        }

        // Show the notification even when in foreground
        completionHandler(UNNotificationPresentationOptions.Banner |
                          UNNotificationPresentationOptions.Sound |
                          UNNotificationPresentationOptions.Badge);
    }

    /// <summary>
    /// Called when user taps on a notification.
    /// </summary>
    [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
    public void DidReceiveNotificationResponse(
        UNUserNotificationCenter center,
        UNNotificationResponse response,
        Action completionHandler)
    {
        var userInfo = response.Notification.Request.Content.UserInfo;
        var pushService = GetPushService();

        if (pushService != null)
        {
            var data = ExtractData(userInfo);
            data.Title = response.Notification.Request.Content.Title;
            data.Body = response.Notification.Request.Content.Body;
            data.Action = response.ActionIdentifier;
            pushService.HandleNotificationTapped(data);
        }

        completionHandler();
    }

    /// <summary>
    /// Called when APNs assigns a device token. Forward to FCM and push service.
    /// </summary>
    public static void DidRegisterForRemoteNotifications(NSData deviceToken)
    {
        // Firebase SDK handles token mapping from APNs → FCM internally.
        // We extract the FCM token via Firebase.CloudMessaging.Messaging.SharedInstance.FcmToken
        // in production. For now, store the APNs token as a hex string fallback.
        var tokenBytes = new byte[deviceToken.Length];
        System.Runtime.InteropServices.Marshal.Copy(deviceToken.Bytes, tokenBytes, 0, (int)deviceToken.Length);
        var tokenString = BitConverter.ToString(tokenBytes).Replace("-", "").ToLowerInvariant();

        var pushService = GetPushService();
        if (pushService != null)
        {
            _ = pushService.OnTokenRefreshedAsync(tokenString);
        }
    }

    private static Services.WatchPushNotificationService? GetPushService()
    {
        return IPlatformApplication.Current?.Services
            .GetService<Services.WatchPushNotificationService>();
    }

    private static Services.PushNotificationData ExtractData(NSDictionary? userInfo)
    {
        var data = new Services.PushNotificationData();
        if (userInfo == null) return data;

        foreach (var key in userInfo.Keys)
        {
            var keyStr = key.ToString();
            var val = userInfo[key]?.ToString() ?? "";
            data.Data[keyStr] = val;
        }

        return data;
    }
}
#endif
