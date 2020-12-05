using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using atomex.CustomElements;
using atomex.iOS.CustomElements;

[assembly: ExportRenderer(typeof(CustomTabbedPage), typeof(CustomTabbedPageRenderer))]
namespace atomex.iOS.CustomElements
{
    public class CustomTabbedPageRenderer : TabbedRenderer
    {
        public CustomTabbedPageRenderer()
        {
        }

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null && e.NewElement is CustomTabbedPage tabbedPage)
                TabBar.BarTintColor = tabbedPage.BackgroundColor.ToUIColor();
        }
    }
}
