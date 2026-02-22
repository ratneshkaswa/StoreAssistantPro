using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Converts a PIN length and a dot index (passed as <c>ConverterParameter</c>)
/// to a <see cref="Brush"/> — filled when the digit has been entered,
/// light grey when it hasn't.
/// <para>
/// Usage: <c>Fill="{Binding PinLength, Converter={StaticResource PinDotConverter}, ConverterParameter=0}"</c>
/// </para>
/// </summary>
public sealed class PinDotConverter : IValueConverter
{
    private static readonly Brush FilledBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
    private static readonly Brush EmptyBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0));

    static PinDotConverter()
    {
        FilledBrush.Freeze();
        EmptyBrush.Freeze();
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int pinLength &&
            parameter is string indexStr &&
            int.TryParse(indexStr, out var dotIndex))
        {
            return pinLength > dotIndex ? FilledBrush : EmptyBrush;
        }

        return EmptyBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
