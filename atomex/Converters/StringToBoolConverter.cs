using System;
using System.Globalization;
using Xamarin.Forms;

namespace atomex.Converters
{
    public class StringToBoolConverter : IValueConverter
    {
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                if (string.IsNullOrEmpty((string)value))
                    return false;
                else
                    return true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
