using Xamarin.Forms;

namespace atomex.CustomElements
{
    public class CustomFrame : Frame
    {
        public static new readonly BindableProperty CornerRadiusProperty =
            BindableProperty.Create(
                nameof(CustomFrame),
                typeof(CornerRadius),
                typeof(CustomFrame));

        public CustomFrame()
        {
            base.CornerRadius = 0;
        }

        public new CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
    }
}
