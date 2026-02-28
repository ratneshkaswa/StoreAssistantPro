using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Converts a PIN strength value (0–3) to a visual brush.
/// ConverterParameter: "0", "1", or "2" for which bar segment.
/// A segment is filled if the strength >= segment+1.
/// </summary>
public class PinStrengthConverter : IValueConverter
{
    private static readonly Brush Empty = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
    private static readonly Brush Weak = new SolidColorBrush(Color.FromRgb(0xC4, 0x2B, 0x1C));
    private static readonly Brush Fair = new SolidColorBrush(Color.FromRgb(0xC1, 0x7A, 0x0E));
    private static readonly Brush Strong = new SolidColorBrush(Color.FromRgb(0x0F, 0x7B, 0x0F));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var strength = value is int s ? s : 0;
        var segment = int.TryParse(parameter as string, out var p) ? p : 0;

        if (strength == 0 || strength <= segment)
            return Empty;

        return strength switch
        {
            1 => Weak,
            2 => Fair,
            _ => Strong
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
