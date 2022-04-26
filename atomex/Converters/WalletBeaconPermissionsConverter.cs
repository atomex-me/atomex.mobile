using System;
using System.Collections.Generic;
using System.Globalization;
using Beacon.Sdk.Beacon.Permission;
using Xamarin.Forms;

namespace atomex.Converters
{
    public class WalletBeaconPermissionsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var permissions = (List<PermissionScope>) value;
            var result = "Permissions: ";

            if (permissions.Count > 0)
                foreach (var permission in permissions)
                    result += permission.ToString() + ' ';

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
