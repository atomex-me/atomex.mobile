using Android.Content;
using Android.Text;
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
            if (Control != null)
            {
                Control.SetPadding(60, -10, 60, 0);
                Control.SetBackgroundColor(Android.Graphics.Color.Transparent);
                this.Control.SetRawInputType(InputTypes.TextFlagNoSuggestions);
            }
        }
    }
}

