using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Gms.Extensions;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Firebase.Messaging;
using Plugin.Fingerprint;
using Sentry;
using Serilog;
using Serilog.Events;
using Xamarin.Forms;
using atomex.Common.FileSystem;
using Atomex.Common;
using Atomex.TzktEvents;

namespace atomex.Droid
{
    [Activity(Label = "Atomex", Icon = "@mipmap/icon", Theme = "@style/MainTheme",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, LaunchMode = LaunchMode.SingleTask,
        ScreenOrientation = ScreenOrientation.Locked)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public static MainActivity Instance { get; private set; }

        protected override void OnCreate(Bundle bundle)
        {
            ConfigureLogging();

            Forms.SetFlags("Brush_Experimental");
            Forms.SetFlags("Shapes_Experimental");

            base.OnCreate(bundle);

            FileSystem.UseFileSystem(new AndroidFileSystem());

            // disable ssl certificate check for TzKT
            TzktEventsClient.ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback;

            //Window.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LightNavigationBar;

            Window.SetNavigationBarColor(
                Android.Graphics.Color.ParseColor(ApplicationContext.Resources.GetString(Resource.Color.colorPrimary)));

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

            _ = GetDeviceTokenAsync();

            AndroidEnvironment.UnhandledExceptionRaiser += AndroidUnhandledExceptionRaiser;

            LoadApplication(new App());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void AndroidUnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            Log.Error(e.Exception, "Android unhandled exception");
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            global::ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions,
                grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private async Task GetDeviceTokenAsync()
        {
            try
            {
                var token = await FirebaseMessaging.Instance.GetToken();

                App.DeviceToken = token.ToString();

                Log.Debug("DeviceToken: {@token}", App.DeviceToken);

                // apply device token to sentry
                SentrySdk.ConfigureScope(scope => { scope.SetTag("device_token", App.DeviceToken); });
            }
            catch (Exception e)
            {
                Log.Error(e, "Get device token error");
            }
        }

        private void ConfigureLogging()
        {
            SentryXamarin.Init(o =>
            {
                o.Dsn = "https://dee6b20f797d4dff97b8bcdbd738a583@newsentry.baking-bad.org/4";
                o.CreateHttpClientHandler = () => new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback
                };
            });

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
#if DEBUG
                .MinimumLevel.Debug()
                .WriteTo.AndroidLog()
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

        private static bool ServerCertificateCustomValidationCallback(
            HttpRequestMessage message,
            X509Certificate2 cert,
            X509Chain chan,
            SslPolicyErrors policyErrors)
        {
            return true;
        }
    }
}