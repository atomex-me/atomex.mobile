using System;
using System.Runtime.InteropServices;
using Atomex.Common;
using Foundation;
using UIKit;
using UserNotifications;

using atomex.Common.FileSystem;
using Xamarin.Forms;
using atomex.Services;
using Serilog.Debugging;
using Serilog;
using Serilog.Events;
using Sentry;

namespace atomex.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate, IUNUserNotificationCenterDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        private string DeviceToken { get; set; }

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            FileSystem.UseFileSystem(new IosFileSystem());

            Forms.SetFlags("Shapes_Experimental");
            Forms.SetFlags("Brush_Experimental");
            Forms.Init();
            ZXing.Net.Mobile.Forms.iOS.Platform.Init();


            // Register your app for remote notifications.
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {

                // For iOS 10 display notification (sent via APNS)
                UNUserNotificationCenter.Current.Delegate = this;

                var authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;
                UNUserNotificationCenter.Current.RequestAuthorization(authOptions, (granted, error) => {
                    Log.Error("No rights granted for notifications");
                });
            }
            else
            {
                // iOS 9 or before
                var allNotificationTypes = UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound;
                var settings = UIUserNotificationSettings.GetSettingsForTypes(allNotificationTypes, null);
                UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
            }

            UIApplication.SharedApplication.RegisterForRemoteNotifications();

            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {

            //DeviceToken = Regex.Replace(deviceToken.ToString(), "[^0-9a-zA-Z]+", "");
            byte[] result = new byte[deviceToken.Length];
            Marshal.Copy(deviceToken.Bytes, result, 0, (int)deviceToken.Length);
            DeviceToken = BitConverter.ToString(result).Replace("-", "");

            App.FileSystem = "iOS";
            App.DeviceToken = DeviceToken;

            StartSentry();
        }

        private void StartSentry()
        {
            SelfLog.Enable(m => Log.Error(m));

            Log.Logger = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .MinimumLevel.Debug()
              .WriteTo.Sentry(o =>
              {
                  o.Dsn = new Dsn("https://ac38520134554ae18e8db1d94c9b51bc@sentry.baking-bad.org/6");
                  o.MinimumEventLevel = LogEventLevel.Error;
                  o.MinimumBreadcrumbLevel = LogEventLevel.Error;
                  o.AttachStacktrace = true;
                  o.SendDefaultPii = true;
                  o.Environment = "iOS: " + DeviceToken;
              })
              .CreateLogger();
        }

        public void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            DependencyService.Get<INotificationManager>().ReceiveNotification(notification.Request.Content.Title, notification.Request.Content.Body);

            // alerts are always shown for demonstration but this can be set to "None"
            // to avoid showing alerts if the app is in the foreground
            var settings = UNNotificationPresentationOptions.Sound | UNNotificationPresentationOptions.Alert;
            completionHandler(settings);
        }

        public void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            switch (response.ActionIdentifier)
            {
                case "custom":
                    // Do something
                    break;
                default:
                    // Take action based on identifier
                    if (response.IsDefaultAction)
                    {
                        // Handle default action...
                        DependencyService.Get<INotificationManager>().RemoveNotifications();
                    }
                    else if (response.IsDismissAction)
                    {
                        // Handle dismiss action
                    }
                    break;
            }
            completionHandler();
        }



        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {

        }

        public override void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
        {

        }

        public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {

        }

        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            completionHandler(UIBackgroundFetchResult.NewData);
        }
    }
}
