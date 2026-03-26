using StoreAssistantPro.Models.Localization;

namespace StoreAssistantPro.Modules.Localization.Services;

/// <summary>
/// Indian number format service — lakh/crore grouping (#576).
/// </summary>
public interface IIndianNumberFormatService
{
    /// <summary>Format a number with Indian grouping: 1,23,45,678.</summary>
    string FormatIndian(decimal number, int decimalPlaces = 2);

    /// <summary>Format a number with Indian grouping as integer.</summary>
    string FormatIndian(long number);

    /// <summary>Convert number to words in Indian style: "One Lakh Twenty-Three Thousand".</summary>
    string ToWords(decimal amount);
}

/// <summary>
/// Regional date format and calendar support (#577, #578).
/// </summary>
public interface IRegionalCalendarService
{
    /// <summary>Get available date format options.</summary>
    IReadOnlyList<string> GetAvailableDateFormats();

    /// <summary>Convert a Gregorian date to a regional calendar date.</summary>
    RegionalCalendarDate ConvertToRegionalCalendar(DateTime gregorianDate, RegionalCalendarType calendarType);

    /// <summary>Format a date with the specified regional format string.</summary>
    string FormatDate(DateTime date, string formatString);

    /// <summary>Get the current date in the specified regional calendar.</summary>
    RegionalCalendarDate GetCurrentRegionalDate(RegionalCalendarType calendarType);
}

/// <summary>
/// State-wise GST tax label resolution (#579).
/// </summary>
public interface IStateTaxLabelService
{
    /// <summary>
    /// Determine tax label based on seller and buyer state codes.
    /// Same state → CGST + SGST; different states → IGST.
    /// </summary>
    TaxLabelResult ResolveTaxLabels(string sellerStateCode, string? buyerStateCode, decimal taxRate);

    /// <summary>Get all Indian state codes and names.</summary>
    IReadOnlyDictionary<string, string> GetStateCodes();
}

/// <summary>
/// Regional receipt template generation (#580).
/// </summary>
public interface IRegionalReceiptService
{
    /// <summary>Get available regional languages for receipts.</summary>
    IReadOnlyList<string> GetAvailableLanguages();

    /// <summary>Get receipt template configuration for a language.</summary>
    RegionalReceiptConfig GetReceiptConfig(string language);

    /// <summary>Generate receipt text in the specified regional language.</summary>
    string GenerateRegionalReceipt(string language, string firmName, string firmAddress,
        IReadOnlyList<(string ItemName, int Quantity, decimal Price)> items,
        decimal total, decimal taxAmount, string? footerText = null);
}
