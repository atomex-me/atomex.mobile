using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Extensions;
using Android.OS;
using Android.Runtime;
using Firebase.Messaging;
using Plugin.Fingerprint;
using Sentry;
using Serilog;
using Serilog.Events;
using Xamarin.Forms;
using atomex.Common.FileSystem;
using Atomex.Common;
using Atomex.TzktEvents;
using Firebase;
using Xamarin.Forms.Platform.Android.AppLinks;

namespace atomex.Droid
{
    [Activity(Label = "Atomex", Icon = "@mipmap/ic_launcher", Theme = "@style/MainTheme",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, LaunchMode = LaunchMode.SingleTask,
        ScreenOrientation = ScreenOrientation.Locked, Exported = true)]
    
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[]
        {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
        },
        DataScheme = "atomex",
        DataPathPrefix = "",
        DataHost = "",
        AutoVerify = true)]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[]
        {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
        },
        DataScheme = "tezos",
        DataPathPrefix = "",
        DataHost = "",
        AutoVerify = true)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public static MainActivity Instance { get; private set; }
        private App _app { get; set; }

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

            Window?.SetNavigationBarColor(
                Android.Graphics.Color.ParseColor(ApplicationContext.Resources.GetString(Resource.Color.colorPrimary)));

            CrossFingerprint.SetCurrentActivityResolver(() => this);

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            Plugin.InputKit.Platforms.Droid.Config.Init(this, bundle);
            Rg.Plugins.Popup.Popup.Init(this);
            Xamarin.Essentials.Platform.Init(this, bundle);
            Forms.Init(this, bundle);
            FirebaseApp.InitializeApp(this);
            AndroidAppLinks.Init(this);

            Instance = this;

            ZXing.Net.Mobile.Forms.Android.Platform.Init();

            App.FileSystem = Device.Android;

            _ = GetDeviceTokenAsync();

            AndroidEnvironment.UnhandledExceptionRaiser += AndroidUnhandledExceptionRaiser;

            _app = new App();
            LoadApplication(_app);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            if (Intent.ActionView != intent.Action || string.IsNullOrWhiteSpace(intent.DataString))
                return;
            
            var type = intent.Data?.GetQueryParameter("type");
            if (string.IsNullOrEmpty(intent.Data?.Host) && type == "tzip10")
            {
                string data = intent.Data?.GetQueryParameter("data");
                _app.OnDeepLinkReceived(data);
            }
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
            [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            for (int i = 0; i < permissions.Length; i++)
            {
                if (permissions[i].Equals("android.permission.CAMERA") && grantResults[i] == Permission.Granted)
                {
                    ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                    _app.AllowCamera();
                }
            }
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private async Task GetDeviceTokenAsync()
        {
            try
            {
                var token = await FirebaseMessaging.Instance.GetToken();

                App.DeviceToken = token.ToString();

                Log.Debug("DeviceToken: {@Token}", App.DeviceToken);

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