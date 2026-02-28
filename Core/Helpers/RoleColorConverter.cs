using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Converts a <see cref="UserType"/> to the matching role-identity brush.
/// <para>
/// ConverterParameter selects the brush variant:
/// <c>"Background"</c> (default), <c>"Border"</c>, or <c>"Accent"</c>.
/// </para>
/// </summary>
public class RoleColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var suffix = parameter as string ?? "Background";

        var key = value switch
        {
            UserType.Admin   => $"RoleAdmin{suffix}",
            UserType.Manager => $"RoleManager{suffix}",
            UserType.User    => $"RoleUser{suffix}",
            _                => null
        };

        if (key is not null && Application.Current.TryFindResource(key) is Brush brush)
            return brush;

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
