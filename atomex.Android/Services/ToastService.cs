using atomex.Services;
using Android.Content;
using Xamarin.Forms;
using Android.Widget;

[assembly: Dependency(typeof(atomex.Droid.Services.ToastService))]
namespace atomex.Droid.Services
{
    public class ToastService : IToastService
    {
        public async void Show(string message, ToastPosition toastPosition, string appTheme)
        {
            Context context = Android.App.Application.Context;
            Toast toast = Toast.MakeText(context, message, ToastLength.Short);

            toast.View.SetBackgroundResource(Resource.Drawable.CustomToast);
            TextView tv = toast.View.FindViewById<TextView>(Android.Resource.Id.Message);
            tv.SetHeight(100);
            tv.Gravity = Android.Views.GravityFlags.Center;

            if (appTheme == "Dark")
                tv.SetTextColor(Android.Graphics.Color.Black);    
            
            else
                tv.SetTextColor(Android.Graphics.Color.White);

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
    }
}
