using atomex.CustomElements;
using atomex.iOS.CustomElements;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(CustomViewCell), typeof(CustomViewCellRenderer))]
namespace atomex.iOS.CustomElements
{
    public class CustomViewCellRenderer : ViewCellRenderer
    {
        public override UITableViewCell GetCell(Cell item, UITableViewCell reusableCell, UITableView tv)
        {
            var cell = base.GetCell(item, reusableCell, tv);
            var view = item as CustomViewCell;

            cell.SelectedBackgroundView = new UIView
            {
                BackgroundColor = view.SelectedBackgroundColor.ToUIColor(),
            };
            return cell;
        }
    }
}
