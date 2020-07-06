using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using atomex.CustomElements;
using atomex.Droid.CustomElements;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CustomPicker), typeof(CustomPickerRenderer))]
namespace atomex.Droid.CustomElements
{
    public class CustomPickerRenderer : PickerRenderer
    {
        public CustomPickerRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
        {
            base.OnElementChanged(e);


            if (Control != null)
            {
                Control.SetHintTextColor(Android.Graphics.Color.White);
                Control.SetSingleLine(true);
                Control.SetTypeface(null, TypefaceStyle.Normal);
                Control.Gravity = GravityFlags.Left;
                Control.SetPadding(30, 60, 30, 60);

                GradientDrawable gd = new GradientDrawable();
                gd.SetStroke(2, Android.Graphics.Color.LightGray);
                gd.SetCornerRadius(6);
                Control.Background = gd;
            }
        }
    }
}

