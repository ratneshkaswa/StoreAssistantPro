using System.Globalization;
using System.Windows.Data;

namespace StoreAssistantPro.Core.Helpers;

public class WindowWidthToColumnsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            if (width < 1450) return 1;
            if (width < 1900) return 2;
            return 3;
        }

        return 3;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
