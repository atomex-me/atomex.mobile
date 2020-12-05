using Xamarin.Forms;

namespace atomex.CustomElements
{
    public class CustomViewCell : ViewCell
    {
        public static readonly BindableProperty SelectedBackgroundColorProperty =
            BindableProperty.Create("SelectedBackgroundColor",
                                typeof(Color),
                                typeof(CustomViewCell),
                                Color.Default);

        public Color SelectedBackgroundColor
        {
            get { return (Color)GetValue(SelectedBackgroundColorProperty); }
            set { SetValue(SelectedBackgroundColorProperty, value); }
        }
    }
}
