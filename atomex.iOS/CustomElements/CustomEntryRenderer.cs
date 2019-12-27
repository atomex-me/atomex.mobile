using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using atomex.CustomElements;
using atomex.iOS;
using System.ComponentModel;
using CoreGraphics;

[assembly: ExportRenderer(typeof(CustomEntry), typeof(CustomEntryRenderer))]
namespace atomex.iOS
{
    public class CustomEntryRenderer : EntryRenderer
    {
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            
            if (Control != null)
            {
                Control.Layer.BorderWidth = 0;
                Control.Layer.CornerRadius = 0;
                Control.BorderStyle = UITextBorderStyle.None;
                Control.Layer.BackgroundColor = UIColor.White.CGColor;

                Control.LeftView = new UIView(new CGRect(0, 0, 15, 0));
                Control.LeftViewMode = UITextFieldViewMode.Always;
                //ANDROID: Control.SetPadding(15, 15, 15, 0);
            }

            if (e.PropertyName == "IsFocused")
            {
                if (Element.IsFocused) {
                    Control.Layer.BorderColor = UIColor.Gray.CGColor;
                    Control.Layer.BorderWidth = 1;
                    Control.Layer.CornerRadius = 10;
                }
            }
        } 
    }
}