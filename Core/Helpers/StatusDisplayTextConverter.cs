using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace StoreAssistantPro.Core.Helpers;

public sealed partial class StatusDisplayTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value?.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = text.Replace('_', ' ').Trim();
        text = CapitalBoundaryRegex().Replace(text, "$1 $2");
        return WhitespaceRegex().Replace(text, " ").Trim();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex CapitalBoundaryRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
