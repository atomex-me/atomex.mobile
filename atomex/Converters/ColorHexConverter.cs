using System;
using System.Globalization;
using Xamarin.Forms;

namespace atomex.Converters
{
    public class ColorHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is string && (string)value != "")
                return Color.FromHex(value.ToString());
            else
                return Color.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
