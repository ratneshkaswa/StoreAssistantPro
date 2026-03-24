using System.Globalization;
using System.Windows.Data;

namespace StoreAssistantPro.Core.Helpers;

public sealed class DoubleLessThanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryReadDouble(value, culture, out var currentWidth)
            || !TryReadDouble(parameter, culture, out var threshold))
        {
            return false;
        }

        return currentWidth < threshold;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static bool TryReadDouble(object? value, CultureInfo culture, out double result)
    {
        if (value is string stringValue)
        {
            if (double.TryParse(stringValue, NumberStyles.Float, culture, out var parsed)
                || double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                result = parsed;
                return true;
            }
        }

        switch (value)
        {
            case double doubleValue:
                result = doubleValue;
                return true;
            case float floatValue:
                result = floatValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            default:
                result = 0;
                return false;
        }
    }
}
