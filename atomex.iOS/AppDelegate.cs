using System;
using System.Runtime.InteropServices;
using Foundation;
using Sentry;
using Serilog;
using UIKit;
using UserNotifications;
using Xamarin.Forms;
using atomex.Common.FileSystem;
using atomex.Services;
using Atomex.Common;

namespace atomex.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate,
        IUNUserNotificationCenterDelegate
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
            ConfigureLogging();

            FileSystem.UseFileSystem(new IosFileSystem());

            Forms.SetFlags("Shapes_Experimental");
            Forms.SetFlags("Brush_Experimental");
            Forms.Init();
            ZXing.Net.Mobile.Forms.iOS.Platform.Init();
            Rg.Plugins.Popup.Popup.Init();

            // Register your app for remote notifications.
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                // For iOS 10 display notification (sent via APNS)
                UNUserNotificationCenter.Current.Delegate = this;

                var authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge |
                                  UNAuthorizationOptions.Sound;
                UNUserNotificationCenter.Current.RequestAuthorization(authOptions,
                    (granted, error) => { Log.Error("No rights granted for notifications"); });
            }
            else
            {
                // iOS 9 or before
                var allNotificationTypes = UIUserNotificationType.Alert | UIUserNotificationType.Badge |
                                           UIUserNotificationType.Sound;
                var settings = UIUserNotificationSettings.GetSettingsForTypes(allNotificationTypes, null);
                UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
            }

            UIApplication.SharedApplication.RegisterForRemoteNotifications();

            LoadApplication(new App());

            Plugin.InputKit.Platforms.iOS.Config.Init();

            return base.FinishedLaunching(app, options);
        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            try
            {
                byte[] result = new byte[deviceToken.Length];
                Marshal.Copy(deviceToken.Bytes, result, 0, (int) deviceToken.Length);
                DeviceToken = BitConverter.ToString(result).Replace("-", "");

                App.FileSystem = Device.iOS;
                App.DeviceToken = DeviceToken;

                // apply device token to sentry
                SentrySdk.ConfigureScope(scope => { scope.SetTag("device_token", DeviceToken); });
            }
            catch (Exception e)
            {
                Log.Error(e, "RegisteredForRemoteNotifications error");
            }
        }

        private void ConfigureLogging()
        {
            SentryXamarin.Init(o => { o.Dsn = "https://dee6b20f797d4dff97b8bcdbd738a583@newsentry.baking-bad.org/4"; });

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
#if DEBUG
                .MinimumLevel.Debug()
                .WriteTo.NSLog()
#else
                .WriteTo.Sentry(o =>
                {
                    //o.TracesSampleRate = 1.0;
                    o.MinimumEventLevel = LogEventLevel.Error;
                    o.MinimumBreadcrumbLevel = LogEventLevel.Error;
                    o.AttachStacktrace = true;
                    //o.SendDefaultPii = true;
                    o.InitializeSdk = false;
                })
#endif
                .CreateLogger();
        }

        public void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
        {
            DependencyService.Get<INotificationManager>().ReceiveNotification(notification.Request.Content.Title,
                notification.Request.Content.Body);

            // alerts are always shown for demonstration but this can be set to "None"
            // to avoid showing alerts if the app is in the foreground
            var settings = UNNotificationPresentationOptions.Sound | UNNotificationPresentationOptions.Alert;
            completionHandler(settings);
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

        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo,
            Action<UIBackgroundFetchResult> completionHandler)
        {
            completionHandler(UIBackgroundFetchResult.NewData);
        }
    }
}