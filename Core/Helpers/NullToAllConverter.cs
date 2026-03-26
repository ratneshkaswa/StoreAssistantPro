using System.Globalization;
using System.Windows.Data;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Converts null to the string "All", otherwise returns the value as-is.
/// Used in ComboBox item templates for filter dropdowns.
/// </summary>
public sealed class NullToAllConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value ?? "All";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
