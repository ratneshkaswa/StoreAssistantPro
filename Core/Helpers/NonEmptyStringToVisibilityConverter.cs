using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Returns <see cref="Visibility.Visible"/> when the bound string value
/// is non-null and non-empty; <see cref="Visibility.Collapsed"/> otherwise.
/// Use for conditionally showing detail labels in panels.
/// </summary>
public class NonEmptyStringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && !string.IsNullOrEmpty(s)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
