using System;
using System.Globalization;
using Xamarin.Forms;

namespace atomex.Converters
{
    public class GreaterThanValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int parameterInt = 0;

            if (parameter != null)
            {
                string parameterString = (string)parameter;

                if (!string.IsNullOrEmpty(parameterString))
                {
                    int.TryParse(parameterString, out parameterInt);
                }
            }

            return ((int)value) >= parameterInt;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
