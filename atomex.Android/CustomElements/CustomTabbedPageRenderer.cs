using Android.Content;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android.AppCompat;

using atomex.CustomElements;
using atomex.Droid.CustomElements;

[assembly: ExportRenderer(typeof(CustomTabbedPage), typeof(CustomTabbedPageRenderer))]
namespace atomex.Droid.CustomElements
{
    public class CustomTabbedPageRenderer : TabbedPageRenderer
    {
        public CustomTabbedPageRenderer(Context context)
            : base(context)
        {
        }
    }
}