using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using atomex.CustomElements;
using atomex.iOS;
using System.ComponentModel;
using CoreGraphics;

[assembly: ExportRenderer(typeof(CustomPicker), typeof(CustomPickerRenderer))]
namespace atomex.iOS
{
    public class CustomPickerRenderer : PickerRenderer
    {
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == "Renderer")
            {
                if (Control != null)
                {
                    Control.Layer.BorderWidth = 0;
                    Control.BorderStyle = UITextBorderStyle.None;

                    var padding = (Element as CustomPicker)?.Padding;
                    if (!padding.HasValue)
                        return;

                    Control.LeftView = new UIView(new CGRect(0, 0, padding.Value.Left, 0));
                    Control.LeftViewMode = UITextFieldViewMode.Always;
                    Control.RightView = new UIView(new CGRect(0, 0, 0, padding.Value.Right));
                    Control.LeftViewMode = UITextFieldViewMode.Always;
                }
            }
        }
    }
}
