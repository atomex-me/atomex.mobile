using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Atomex.Common;
using atomex.Common.FileSystem;
using Android.Util;
using Plugin.Fingerprint;
using Firebase.Iid;
using Serilog.Debugging;
using Serilog;
using Serilog.Events;
using Sentry;
using Log = Serilog.Log;

namespace atomex.Droid
{
    [Activity(Label = "Atomex", Icon = "@mipmap/icon", Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, LaunchMode = LaunchMode.SingleTask, ScreenOrientation = ScreenOrientation.Locked)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            FileSystem.UseFileSystem(new AndroidFileSystem());

            CrossFingerprint.SetCurrentActivityResolver(() => this);

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            if (Intent.Extras != null)
            {
                foreach (var key in Intent.Extras.KeySet())
                {
                    if (key != null)
                    {
                        var value = Intent.Extras.GetString(key);
                        Log.Debug("Key: {0} Value: {1}", key, value);
                    }
                }
            }
            CreateNotificationFromIntent(Intent);

            Xamarin.Forms.Forms.SetFlags("Shapes_Experimental");

            Xamarin.Essentials.Platform.Init(this, bundle);
            global::Xamarin.Forms.Forms.Init(this, bundle);

            StartSentry();
            AndroidEnvironment.UnhandledExceptionRaiser += AndroidUnhandledExceptionRaiser;

            LoadApplication(new App());
        }

        private void AndroidUnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            SentrySdk.CaptureException(e.Exception);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            global::ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        // clicked on push message
        protected override void OnNewIntent(Intent intent)
        {
            //if (intent.HasExtra("SomeSpecialKey"))
            //{
            //    CreateNotificationFromIntent(intent);
            //}
            CreateNotificationFromIntent(intent);
            OnNotificationClicked(intent);
        }

        void CreateNotificationFromIntent(Intent intent)
        {
            // intent.Extras == null in Background !!!
            if (intent?.Extras != null)
            {
                //if (intent.Extras.ContainsKey(AndroidNotificationManager.AlertKey))
                //{
                //    if (intent.Extras.GetString(AndroidNotificationManager.AlertKey) == "true" &&
                //        intent.Extras.ContainsKey(AndroidNotificationManager.SwapIdKey))
                //    {
                //        DependencyService.Get<INotificationManager>().ReceiveNotification(
                //            "Atomex",
                //            string.Format("Login to the application to complete the swap transaction {0}", intent.Extras.GetString(AndroidNotificationManager.SwapIdKey)));
                //    }
                //}
            }
        }

        void OnNotificationClicked(Intent intent)
        {
            if (intent.Action == "Swap" && intent.HasExtra("swapId"))
            {
                /// Do something now that you know the user clicked on the notification...
            }
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
                  o.Environment = "Android: " + FirebaseInstanceId.Instance.Token;
              })
              .CreateLogger();
        }
    }
}