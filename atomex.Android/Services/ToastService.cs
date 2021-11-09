using atomex.Services;
using Android.Content;
using Xamarin.Forms;
using Android.Widget;
using System;
using Serilog;

[assembly: Dependency(typeof(atomex.Droid.Services.ToastService))]
namespace atomex.Droid.Services
{
    public class ToastService : IToastService
    {
        public void Show(string message, ToastPosition toastPosition, string appTheme)
        {
            try
            {
                Context context = Android.App.Application.Context;
                Toast toast = Toast.MakeText(context, message, ToastLength.Short);

                if (Xamarin.Essentials.DeviceInfo.Version.Major <= 10)
                {
                    toast.View.SetBackgroundResource(Resource.Drawable.CustomToast);
                    toast.SetGravity(Android.Views.GravityFlags.Center, 0, 0);
                    TextView tv = toast.View.FindViewById<TextView>(Android.Resource.Id.Message);
                    tv.Gravity = Android.Views.GravityFlags.CenterVertical | Android.Views.GravityFlags.CenterHorizontal;
                    tv.SetPadding(20, 0, 20, 0);

                    if (appTheme == "Dark")
                        tv.SetTextColor(Android.Graphics.Color.Black);
                    else
                        tv.SetTextColor(Android.Graphics.Color.White);
                }

                switch (toastPosition)
                {
                    case ToastPosition.Top:
                        toast.SetGravity(Android.Views.GravityFlags.Top, 0, 200);
                        break;
                    case ToastPosition.Center:
                        toast.SetGravity(Android.Views.GravityFlags.CenterVertical, 0, 0);
                        break;
                    case ToastPosition.Bottom:
                        toast.SetGravity(Android.Views.GravityFlags.Bottom, 0, 200);
                        break;
                    default:
                        toast.SetGravity(Android.Views.GravityFlags.Top, 0, 200);
                        break;
                }
                toast.Show();
            }
            catch (Exception e)
            {
                Log.Error(e, "Show toast error");
            }
        }
    }
}
