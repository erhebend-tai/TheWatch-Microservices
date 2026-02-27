using Foundation;
using UIKit;
using TheWatch.Mobile.Platforms.iOS;

namespace TheWatch.Mobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var result = base.FinishedLaunching(application, launchOptions);
        WatchPushNotificationDelegate.Register();
        return result;
    }

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        WatchPushNotificationDelegate.DidRegisterForRemoteNotifications(deviceToken);
    }
}
