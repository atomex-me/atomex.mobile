using System;
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

        public static readonly BindableProperty BindableHeightProperty =
            BindableProperty.Create("BindableHeight",
                                typeof(double),
                                typeof(CustomViewCell),
                                0d,
                                propertyChanging: (bindable, oldValue, newValue) =>
                                {
                                    var cell = bindable as ViewCell;
                                    cell.Height = (double)newValue;
                                });

        public double BindableHeight
        {
            get { return (double)GetValue(BindableHeightProperty); }
            set { SetValue(BindableHeightProperty, value); }
        }
    }
}
