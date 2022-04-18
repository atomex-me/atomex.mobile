﻿using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Atomex.Common;
using atomex.Common.FileSystem;
using Plugin.Fingerprint;
using Android.Views;
using Xamarin.Forms;
using Firebase.Messaging;
using Android.Gms.Extensions;
using System.Threading.Tasks;
using Serilog.Debugging;
using Serilog;
using Serilog.Events;
using Sentry;

namespace atomex.Droid
{
    [Activity(Label = "Atomex", Icon = "@mipmap/icon", Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, LaunchMode = LaunchMode.SingleTask, ScreenOrientation = ScreenOrientation.Locked)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public static MainActivity Instance { get; private set; }

        protected override void OnCreate(Bundle bundle)
        {
            Forms.SetFlags("Brush_Experimental");
            Forms.SetFlags("Shapes_Experimental");

            base.OnCreate(bundle);

            FileSystem.UseFileSystem(new AndroidFileSystem());

            //Window.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LightNavigationBar;

            Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor(ApplicationContext.Resources.GetString(Resource.Color.colorPrimary)));

            CrossFingerprint.SetCurrentActivityResolver(() => this);

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            Plugin.InputKit.Platforms.Droid.Config.Init(this, bundle);
            Rg.Plugins.Popup.Popup.Init(this);
            Xamarin.Essentials.Platform.Init(this, bundle);
            Forms.Init(this, bundle);

            Instance = this;

            global::ZXing.Net.Mobile.Forms.Android.Platform.Init();

            App.FileSystem = Device.Android;
            _ = GetDeviceToken();

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

        private async Task GetDeviceToken()
        {
            string token = (await FirebaseMessaging.Instance.GetToken()).ToString();
            App.DeviceToken = token;
            //StartSentry();
        }

        private void StartSentry()
        {
            SelfLog.Enable(m => Log.Error(m));
            SentrySdk.Init("https://newsentry.baking-bad.org/api/4/?sentry_key=dee6b20f797d4dff97b8bcdbd738a583");

            //SentryXamarin.Init(o =>
            //{
            //    o.Dsn = "https://newsentry.baking-bad.org/api/4/?sentry_key=dee6b20f797d4dff97b8bcdbd738a583";
            //    o.Debug = true;
            //});

            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("platform", "android");
                scope.SetTag("device_token", App.DeviceToken);
            });

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Sentry(o =>
                {
                    o.TracesSampleRate = 1.0;
                    o.MinimumEventLevel = LogEventLevel.Error;
                    o.MinimumBreadcrumbLevel = LogEventLevel.Error;
                    o.AttachStacktrace = true;
                    o.SendDefaultPii = true;
                    o.InitializeSdk = false;
                }).CreateLogger();
        }
    }
}