using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Xamarin.Forms;
using atomex.Services;
using Atomex.Common;
using atomex.Common.FileSystem;
using Android.Util;

namespace atomex.Droid
{
    [Activity(Label = "Atomex", Icon = "@mipmap/icon", Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, LaunchMode = LaunchMode.SingleTask, ScreenOrientation = ScreenOrientation.Locked)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            FileSystem.UseFileSystem(new AndroidFileSystem());

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

            Xamarin.Essentials.Platform.Init(this, bundle);
            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        // clicked on push message
        protected override void OnNewIntent(Intent intent)
        {
            //if (intent.HasExtra("SomeSpecialKey"))
            //{
            //    System.Diagnostics.Debug.WriteLine("\nIn MainActivity.OnNewIntent() - Intent Extras: " + intent.GetStringExtra("SomeSpecialKey") + "\n");
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
                long swapId = intent.Extras.GetLong(AndroidNotificationManager.SwapIdKey);
                string currency = intent.Extras.GetString(AndroidNotificationManager.CurrencyKey);
                string txId = intent.Extras.GetString(AndroidNotificationManager.TxIdKey);
                string pushType = intent.Extras.GetString(AndroidNotificationManager.PushTypeKey);

                DependencyService.Get<INotificationManager>().ReceiveNotification(swapId, currency, txId, pushType);
            }
        }

        void OnNotificationClicked(Intent intent)
        {
            if (intent.Action == "Swap" && intent.HasExtra("swapId"))
            {
                /// Do something now that you know the user clicked on the notification...
            }
        }
    }
}