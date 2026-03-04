using System.Globalization;
using System.Windows.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Formats a price value, showing "No limit" when the value equals
/// <see cref="TaxSlab.MaxPrice"/> (the sentinel for unbounded slabs).
/// </summary>
[ValueConversion(typeof(decimal), typeof(string))]
public sealed class PriceToDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal price && price >= TaxSlab.MaxPrice)
            return "No limit";

        return value is decimal d ? d.ToString("₹#,##0", culture) : string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
