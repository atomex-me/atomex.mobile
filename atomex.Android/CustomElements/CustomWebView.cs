using Android.Content;
using atomex.CustomElements;
using atomex.Droid.CustomElements;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CustomWebView), typeof(CustomWebViewRenderer))]
namespace atomex.Droid.CustomElements
{
    public class CustomWebViewRenderer : WebViewRenderer
    {
        public CustomWebViewRenderer(Context context) : base(context) { }

        protected override FormsWebChromeClient GetFormsWebChromeClient()
        {
            return new CameraFormsWebChromeClient();
        }
    }
}
