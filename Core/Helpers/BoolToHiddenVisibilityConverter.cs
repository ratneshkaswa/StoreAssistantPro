using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Converts <c>true</c> → <see cref="Visibility.Visible"/>,
/// <c>false</c> → <see cref="Visibility.Hidden"/>.
/// Unlike <see cref="BooleanToVisibilityConverter"/> which returns
/// <c>Collapsed</c>, this preserves the element's layout space —
/// critical for preventing layout shift when toggling overlapping panels.
/// </summary>
public class BoolToHiddenVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Hidden;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}
