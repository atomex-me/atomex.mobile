using Android.Content;
using atomex.CustomElements;
using atomex.Droid.CustomElements;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(LeftRoundedButton), typeof(LeftRoundedButtonRenderer))]
namespace atomex.Droid.CustomElements
{
    public class LeftRoundedButtonRenderer : ButtonRenderer
    {
        public LeftRoundedButtonRenderer(Context context) : base(context)
        {

        }
        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.Button> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.SetBackgroundResource(Resource.Drawable.LeftRoundedButton);
            }
        }
    }
}

