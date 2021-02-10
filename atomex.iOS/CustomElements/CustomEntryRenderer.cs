using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using atomex.CustomElements;
using atomex.iOS;
using System.ComponentModel;
using CoreGraphics;
using System.Drawing;

[assembly: ExportRenderer(typeof(CustomEntry), typeof(CustomEntryRenderer))]
namespace atomex.iOS
{
    public class CustomEntryRenderer : EntryRenderer
    {
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
      
            if (e.PropertyName == "Renderer")
            {
                if (Control != null)
                {
                    Control.BorderStyle = UITextBorderStyle.None;

                    var padding = (Element as CustomEntry)?.Padding;
                    if (!padding.HasValue)
                        return;

                    Control.LeftView = new UIView(new CGRect(0, 0, padding.Value.Left, 0));
                    Control.LeftViewMode = UITextFieldViewMode.Always;
                    Control.RightView = new UIView(new CGRect(0, 0, 0, padding.Value.Right));
                    Control.LeftViewMode = UITextFieldViewMode.Always;
                }
            }
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);
            if (Element == null)
                return;
            // Check only for Numeric keyboard
            if (this.Element.Keyboard == Keyboard.Numeric)
                this.AddDoneButton();
        }
        /// <summary>
        /// <para>Add toolbar with Done button</para>
        /// </summary>
        protected void AddDoneButton()
        {
            var toolbar = new UIToolbar(new RectangleF(0.0f, 0.0f, 50.0f, 44.0f));
            var doneButton = new UIBarButtonItem(UIBarButtonSystemItem.Done, delegate
            {
                this.Control.ResignFirstResponder();
                var baseEntry = this.Element.GetType();
                ((IEntryController)Element).SendCompleted();
            });
            toolbar.Items = new UIBarButtonItem[]
            {
                new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace),
                doneButton
            };
            this.Control.InputAccessoryView = toolbar;
        }
    }

}