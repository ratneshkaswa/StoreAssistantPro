using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StoreAssistantPro.Core.Helpers;

public class EqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is null || values[1] is null)
            return targetType == typeof(Visibility) ? Visibility.Collapsed : false;

        var equal = string.Equals(values[0].ToString(), values[1].ToString(), StringComparison.Ordinal);

        if (targetType == typeof(Visibility))
            return equal ? Visibility.Visible : Visibility.Collapsed;

        return equal;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
