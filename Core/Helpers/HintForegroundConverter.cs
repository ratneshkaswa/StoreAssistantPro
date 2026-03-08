using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Returns FluentSuccess when hint starts with ✓, FluentError when starts with ✗,
/// FluentWarning for other non-empty hints.
/// Falls back to FluentTextSecondary if resources are unavailable.
/// </summary>
public class HintForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string ?? string.Empty;
        var resourceKey = text.StartsWith('✓') ? "FluentSuccess"
            : text.StartsWith('✗') ? "FluentError"
            : "FluentWarning";
        return Application.Current.TryFindResource(resourceKey) as Brush
            ?? Application.Current.TryFindResource("FluentTextSecondary") as Brush
            ?? Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
