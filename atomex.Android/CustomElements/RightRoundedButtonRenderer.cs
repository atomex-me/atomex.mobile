using Android.Content;
using atomex.CustomElements;
using atomex.Droid.CustomElements;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(RightRoundedButton), typeof(RightRoundedButtonRenderer))]
namespace atomex.Droid.CustomElements
{
    public class RightRoundedButtonRenderer : ButtonRenderer
    {
        public RightRoundedButtonRenderer(Context context) : base(context)
        {

        }
        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.Button> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.SetBackgroundResource(Resource.Drawable.RightRoundedButton);
            }
        }
    }
}

