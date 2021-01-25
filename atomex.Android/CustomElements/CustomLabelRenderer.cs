using System;
using Android.Content;
using Android.Runtime;
using atomex.Droid.CustomElements;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android.FastRenderers;

[assembly: ExportRenderer(typeof(Label), typeof(CustomLabelRenderer))]
namespace atomex.Droid.CustomElements
{
    public class CustomLabelRenderer : LabelRenderer
    {
        public CustomLabelRenderer(Context context) : base(context)
        {

        }

        [Obsolete]
        public CustomLabelRenderer(IntPtr handle, JniHandleOwnership transfer)
        {

        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            catch (Exception)
            {
            }
        }

        protected override void OnAttachedToWindow()
        {
            try
            {
                base.OnAttachedToWindow();
            }
            catch (Exception)
            {
            }
        }
    }
}
