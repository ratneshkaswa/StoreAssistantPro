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

    public string CurrencySymbol => "₹";
    public string DateFormat => "dd-MM-yyyy";
    public string TimeFormat => "hh:mm tt";
    public string DateTimeFormat => "dd-MM-yyyy hh:mm tt";
    public TimeZoneInfo TimeZone => Timezone;

    public string FormatCurrency(decimal amount) =>
        amount.ToString("C", Culture);

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
