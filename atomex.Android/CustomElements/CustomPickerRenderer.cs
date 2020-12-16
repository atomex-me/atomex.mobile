using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
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

            if (e.NewElement == null)
            {
                return;
            }

            if (this.Element is CustomPicker customPicker)
            {
                if (Control != null)
                {
                    var paddingLeft = (int)customPicker.Padding.Left;
                    var paddingTop = (int)customPicker.Padding.Top;
                    var paddingRight = (int)customPicker.Padding.Right;
                    var paddingBottom = (int)customPicker.Padding.Bottom;

                    int dpLeftValue = (int)TypedValue.ApplyDimension(
                                ComplexUnitType.Dip,
                                paddingLeft,
                                Context.Resources.DisplayMetrics);
                    int dpRightValue = (int)TypedValue.ApplyDimension(
                                ComplexUnitType.Dip,
                                paddingRight,
                                Context.Resources.DisplayMetrics);
                    int dpTopValue = (int)TypedValue.ApplyDimension(
                                ComplexUnitType.Dip,
                                paddingTop,
                                Context.Resources.DisplayMetrics);
                    int dpBottomValue = (int)TypedValue.ApplyDimension(
                                ComplexUnitType.Dip,
                                paddingBottom,
                                Context.Resources.DisplayMetrics);

                    Control.SetPadding(dpLeftValue, dpTopValue, dpRightValue, dpBottomValue);

                    Control.SetHintTextColor(Android.Graphics.Color.Transparent);
                    Control.SetSingleLine(true);
                    Control.SetTypeface(null, TypefaceStyle.Normal);
                    Control.Gravity = GravityFlags.CenterVertical;

                    GradientDrawable gd = new GradientDrawable();
                    //gd.SetStroke(2, Android.Graphics.Color.LightGray);
                    Control.Background = gd;
                }
            }
        }
    }
}

