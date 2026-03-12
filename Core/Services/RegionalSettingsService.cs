using System.Globalization;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Indian regional defaults. Singleton — injected into any service
/// or ViewModel that needs formatted display strings.
/// <para>
/// To support multiple regions in the future, extract the hardcoded
/// values into <c>appsettings.json</c> and bind them here at startup.
/// </para>
/// </summary>
public class RegionalSettingsService : IRegionalSettingsService
{
    private static readonly CultureInfo Culture = new("en-IN");
    private static readonly TimeZoneInfo Timezone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private readonly Lock _lock = new();
    private string _currencySymbol = "₹";
    private string _dateFormat = "dd-MM-yyyy";

    public string CurrencySymbol { get { lock (_lock) return _currencySymbol; } }
    public string DateFormat { get { lock (_lock) return _dateFormat; } }
    public string TimeFormat => "hh:mm tt";
    public string DateTimeFormat => $"{DateFormat} {TimeFormat}";
    public TimeZoneInfo TimeZone => Timezone;

    public void UpdateSettings(string currencySymbol, string dateFormat)
    {
        lock (_lock)
        {
            if (!string.IsNullOrWhiteSpace(currencySymbol))
                _currencySymbol = currencySymbol;
            if (!string.IsNullOrWhiteSpace(dateFormat))
                _dateFormat = dateFormat;
        }
    }

    public string FormatCurrency(decimal amount)
    {
        string currencySymbol;
        lock (_lock)
            currencySymbol = _currencySymbol;

        var culture = (CultureInfo)Culture.Clone();
        culture.NumberFormat.CurrencySymbol = currencySymbol;
        return amount.ToString("C", culture);
    }

    public string FormatNumber(decimal number) =>
        number.ToString("N2", Culture);

    public string FormatNumber(int number) =>
        number.ToString("N0", Culture);

    public string FormatQuantity(decimal quantity) =>
        quantity == Math.Truncate(quantity)
            ? quantity.ToString("N0", Culture)
            : quantity.ToString("N2", Culture);

    public string FormatPercent(decimal value) =>
        (value / 100m).ToString("P2", Culture);

    public string FormatDate(DateTime date) =>
        date.ToString(DateFormat, Culture);

    public string FormatTime(DateTime time) =>
        time.ToString(TimeFormat, Culture);

    public string FormatDateTime(DateTime dateTime) =>
        dateTime.ToString(DateTimeFormat, Culture);

    public DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Timezone);
}
