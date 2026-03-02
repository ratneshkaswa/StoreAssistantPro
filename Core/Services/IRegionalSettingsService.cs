namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Centralizes regional formatting rules for the application.
/// All display formatting (currency, date, time) must go through
/// this service so regional changes apply in one place.
/// <para>
/// <b>Architecture rule:</b> Never hardcode format strings like
/// <c>"hh:mm tt"</c> or <c>"dd-MM-yyyy"</c> in ViewModels, services,
/// or events. Use <see cref="IRegionalSettingsService"/> helpers instead.
/// XAML <c>StringFormat=C</c> is fine — it reads from <c>CultureInfo</c>
/// which is already set globally in <c>App.xaml.cs</c>.
/// </para>
/// </summary>
public interface IRegionalSettingsService
{
    string CurrencySymbol { get; }
    string DateFormat { get; }
    string TimeFormat { get; }
    string DateTimeFormat { get; }
    TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// Updates the display settings from the Firm configuration.
    /// Called after login and after firm settings are saved.
    /// </summary>
    void UpdateSettings(string currencySymbol, string dateFormat);

    string FormatCurrency(decimal amount);
    string FormatNumber(decimal number);
    string FormatNumber(int number);
    string FormatQuantity(decimal quantity);
    string FormatPercent(decimal value);
    string FormatDate(DateTime date);
    string FormatTime(DateTime time);
    string FormatDateTime(DateTime dateTime);
    DateTime Now { get; }
}
