using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using atomex.CustomElements;
using atomex.iOS;
using CoreGraphics;
using CoreAnimation;

[assembly: ExportRenderer(typeof(RightRoundedButton), typeof(RightRoundedButtonRenderer))]
namespace atomex.iOS
{
    public class RightRoundedButtonRenderer : ButtonRenderer
    {
        public override void LayoutSubviews()
        {
            var maskingShapeLayer = new CAShapeLayer()
            {
                Path = UIBezierPath.FromRoundedRect(Bounds, UIRectCorner.BottomRight | UIRectCorner.TopRight, new CGSize(Layer.Frame.Size.Width / 2, Layer.Frame.Size.Height / 2)).CGPath
            };
            Layer.Mask = maskingShapeLayer;
            base.LayoutSubviews();
        }
    }
}

