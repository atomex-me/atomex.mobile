using Foundation;
using UIKit;
using UserNotifications;

namespace atomex.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            ZXing.Net.Mobile.Forms.iOS.Platform.Init();

            //UIApplication.SharedApplication.RegisterForRemoteNotifications();

            //after iOS 10
            //if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            //{
            //    UNUserNotificationCenter center = UNUserNotificationCenter.Current;

            //    center.RequestAuthorization(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge, (bool arg1, NSError arg2) =>
            //    {

            //    });

            //    center.Delegate = new iOSNotificationReceiver();
            //}

            //else if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            //{

            //    var settings = UIUserNotificationSettings.GetSettingsForTypes(UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound, new NSSet());

            //    UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);

            //}

            UNUserNotificationCenter.Current.Delegate = new iOSNotificationReceiver();
            
            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }

        //public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        //{
        //    base.ReceivedRemoteNotification(application, userInfo);
        //}
    }
}
