using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Converts a positive numeric value to Visible, zero or negative to Collapsed.
/// </summary>
public sealed class PositiveToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i when i > 0 => Visibility.Visible,
            long l when l > 0 => Visibility.Visible,
            double d when d > 0 => Visibility.Visible,
            decimal m when m > 0 => Visibility.Visible,
            _ => Visibility.Collapsed
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
