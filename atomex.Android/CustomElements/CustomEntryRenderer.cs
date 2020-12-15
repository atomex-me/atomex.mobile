using Android.Content;
using Android.Text;
using Android.Util;
using atomex.CustomElements;
using atomex.Droid.CustomElements;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CustomEntry), typeof(CustomEntryRenderer))]
namespace atomex.Droid.CustomElements
{
    public class CustomEntryRenderer : EntryRenderer
    {
        public CustomEntryRenderer(Context context) : base(context)
        {
            AutoPackage = false;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement == null)
            {
                return;
            }

            if (this.Element is CustomEntry customEntry)
            {
                var paddingLeft = (int)customEntry.Padding.Left;
                var paddingTop = (int)customEntry.Padding.Top;
                var paddingRight = (int)customEntry.Padding.Right;
                var paddingBottom = (int)customEntry.Padding.Bottom;

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

                this.Control.SetPadding(dpLeftValue, dpTopValue, dpRightValue, dpBottomValue);


                Control.SetBackgroundColor(Android.Graphics.Color.Transparent);
                this.Control.SetRawInputType(InputTypes.TextFlagNoSuggestions);
            }
        }
    }
}

