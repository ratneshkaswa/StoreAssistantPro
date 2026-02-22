using System.Globalization;
using System.Windows.Data;

namespace StoreAssistantPro.Core.Helpers;

public class BoolToActiveTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? "Active" : "Inactive";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
