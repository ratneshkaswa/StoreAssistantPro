using System.Globalization;
using System.Windows.Data;

namespace StoreAssistantPro.Core.Helpers;

public sealed class RelativeDateTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        DateTime timestamp = value switch
        {
            DateTime dateTime => dateTime,
            DateTimeOffset dateTimeOffset => dateTimeOffset.LocalDateTime,
            _ => default
        };

        if (timestamp == default)
            return string.Empty;

        if (parameter is string fullParameter &&
            fullParameter.Equals("Full", StringComparison.OrdinalIgnoreCase))
        {
            return timestamp.ToString("f", culture);
        }

        var now = DateTime.Now;
        var delta = now - timestamp;

        if (delta < TimeSpan.Zero)
            return timestamp.ToString("g", culture);

        if (delta < TimeSpan.FromMinutes(1))
            return "Just now";

        if (delta < TimeSpan.FromHours(1))
            return $"{Math.Max(1, (int)delta.TotalMinutes)} min ago";

        if (timestamp.Date == now.Date)
            return $"{Math.Max(1, (int)delta.TotalHours)} hr ago";

        if (timestamp.Date == now.Date.AddDays(-1))
            return "Yesterday";

        return timestamp.ToString("g", culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
