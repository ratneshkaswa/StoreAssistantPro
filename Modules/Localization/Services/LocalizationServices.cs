using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models.Localization;

namespace StoreAssistantPro.Modules.Localization.Services;

public sealed class IndianNumberFormatService : IIndianNumberFormatService
{
    private static readonly CultureInfo Indian = new("en-IN");

    public string FormatIndian(decimal number, int decimalPlaces = 2)
        => number.ToString($"N{decimalPlaces}", Indian);

    public string FormatIndian(long number)
        => number.ToString("N0", Indian);

    public string ToWords(decimal amount)
    {
        var intPart = (long)Math.Floor(Math.Abs(amount));
        var sb = new StringBuilder();
        if (amount < 0) sb.Append("Minus ");

        if (intPart == 0) return "Zero";

        if (intPart >= 10_00_00_000) { sb.Append($"{intPart / 10_00_00_000} Arab "); intPart %= 10_00_00_000; }
        if (intPart >= 1_00_00_000) { sb.Append($"{intPart / 1_00_00_000} Crore "); intPart %= 1_00_00_000; }
        if (intPart >= 1_00_000) { sb.Append($"{intPart / 1_00_000} Lakh "); intPart %= 1_00_000; }
        if (intPart >= 1_000) { sb.Append($"{intPart / 1_000} Thousand "); intPart %= 1_000; }
        if (intPart >= 100) { sb.Append($"{intPart / 100} Hundred "); intPart %= 100; }
        if (intPart > 0) sb.Append(intPart);

        return sb.ToString().Trim();
    }
}

public sealed class RegionalCalendarService(IRegionalSettingsService regional) : IRegionalCalendarService
{
    public IReadOnlyList<string> GetAvailableDateFormats() =>
        ["dd-MM-yyyy", "dd/MM/yyyy", "MM-dd-yyyy", "yyyy-MM-dd", "dd MMM yyyy", "dd MMMM yyyy"];

    public RegionalCalendarDate ConvertToRegionalCalendar(DateTime gregorianDate, RegionalCalendarType calendarType) =>
        calendarType switch
        {
            RegionalCalendarType.VikramSamvat => new RegionalCalendarDate(calendarType,
                gregorianDate.Year + 57, gregorianDate.Month, gregorianDate.Day,
                CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(gregorianDate.Month),
                $"{gregorianDate.Day} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(gregorianDate.Month)} {gregorianDate.Year + 57} VS"),
            RegionalCalendarType.Saka => new RegionalCalendarDate(calendarType,
                gregorianDate.Year - 78, gregorianDate.Month, gregorianDate.Day,
                CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(gregorianDate.Month),
                $"{gregorianDate.Day} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(gregorianDate.Month)} {gregorianDate.Year - 78} SE"),
            _ => new RegionalCalendarDate(calendarType, gregorianDate.Year, gregorianDate.Month, gregorianDate.Day,
                CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(gregorianDate.Month),
                gregorianDate.ToString("dd MMMM yyyy"))
        };

    public string FormatDate(DateTime date, string formatString) => date.ToString(formatString, CultureInfo.CurrentCulture);

    public RegionalCalendarDate GetCurrentRegionalDate(RegionalCalendarType calendarType)
        => ConvertToRegionalCalendar(regional.Now, calendarType);
}

public sealed class StateTaxLabelService : IStateTaxLabelService
{
    private static readonly Dictionary<string, string> StateCodes = new()
    {
        ["01"] = "Jammu & Kashmir", ["02"] = "Himachal Pradesh", ["03"] = "Punjab",
        ["04"] = "Chandigarh", ["05"] = "Uttarakhand", ["06"] = "Haryana", ["07"] = "Delhi",
        ["08"] = "Rajasthan", ["09"] = "Uttar Pradesh", ["10"] = "Bihar",
        ["11"] = "Sikkim", ["12"] = "Arunachal Pradesh", ["13"] = "Nagaland",
        ["14"] = "Manipur", ["15"] = "Mizoram", ["16"] = "Tripura", ["17"] = "Meghalaya",
        ["18"] = "Assam", ["19"] = "West Bengal", ["20"] = "Jharkhand",
        ["21"] = "Odisha", ["22"] = "Chhattisgarh", ["23"] = "Madhya Pradesh",
        ["24"] = "Gujarat", ["26"] = "Dadra & Nagar Haveli and Daman & Diu",
        ["27"] = "Maharashtra", ["28"] = "Andhra Pradesh (Old)", ["29"] = "Karnataka",
        ["30"] = "Goa", ["31"] = "Lakshadweep", ["32"] = "Kerala",
        ["33"] = "Tamil Nadu", ["34"] = "Puducherry", ["35"] = "Andaman & Nicobar",
        ["36"] = "Telangana", ["37"] = "Andhra Pradesh"
    };

    public TaxLabelResult ResolveTaxLabels(string sellerStateCode, string? buyerStateCode, decimal taxRate)
    {
        var isInterState = !string.IsNullOrWhiteSpace(buyerStateCode) && sellerStateCode != buyerStateCode;
        var halfRate = taxRate / 2;
        return new TaxLabelResult(isInterState,
            "CGST", "SGST", "IGST",
            isInterState ? 0 : halfRate,
            isInterState ? 0 : halfRate,
            isInterState ? taxRate : 0);
    }

    public IReadOnlyDictionary<string, string> GetStateCodes() => StateCodes;
}

public sealed class RegionalReceiptService : IRegionalReceiptService
{
    private static readonly Dictionary<string, RegionalReceiptConfig> Configs = new()
    {
        ["Hindi"] = new("Hindi", "बिल", "{Name} × {Qty} = ₹{Price}", "कुल", "कर", "धन्यवाद", "आपकी खरीदारी के लिए धन्यवाद!"),
        ["Tamil"] = new("Tamil", "ரசீது", "{Name} × {Qty} = ₹{Price}", "மொத்தம்", "வரி", "நன்றி", "உங்கள் வாங்கலுக்கு நன்றி!"),
        ["Telugu"] = new("Telugu", "రసీదు", "{Name} × {Qty} = ₹{Price}", "మొత్తం", "పన్ను", "ధన్యవాదాలు", "మీ కొనుగోలుకు ధన్యవాదాలు!"),
        ["Kannada"] = new("Kannada", "ರಸೀದಿ", "{Name} × {Qty} = ₹{Price}", "ಒಟ್ಟು", "ತೆರಿಗೆ", "ಧನ್ಯವಾದ", "ನಿಮ್ಮ ಖರೀದಿಗೆ ಧನ್ಯವಾದಗಳು!"),
        ["English"] = new("English", "Receipt", "{Name} × {Qty} = ₹{Price}", "Total", "Tax", "Thank You", "Thank you for your purchase!")
    };

    public IReadOnlyList<string> GetAvailableLanguages() => [.. Configs.Keys];

    public RegionalReceiptConfig GetReceiptConfig(string language)
        => Configs.TryGetValue(language, out var c) ? c : Configs["English"];

    public string GenerateRegionalReceipt(string language, string firmName, string firmAddress,
        IReadOnlyList<(string ItemName, int Quantity, decimal Price)> items,
        decimal total, decimal taxAmount, string? footerText = null)
    {
        var config = GetReceiptConfig(language);
        var sb = new StringBuilder();
        sb.AppendLine($"=== {config.HeaderTemplate} ===");
        sb.AppendLine(firmName);
        sb.AppendLine(firmAddress);
        sb.AppendLine(new string('-', 40));

        foreach (var (name, qty, price) in items)
            sb.AppendLine(config.ItemLineTemplate
                .Replace("{Name}", name)
                .Replace("{Qty}", qty.ToString())
                .Replace("{Price}", price.ToString("N2")));

        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"{config.TotalLabel}: ₹{total:N2}");
        sb.AppendLine($"{config.TaxLabel}: ₹{taxAmount:N2}");
        sb.AppendLine(new string('=', 40));
        sb.AppendLine(config.ThankYouMessage);
        if (!string.IsNullOrWhiteSpace(footerText)) sb.AppendLine(footerText);
        return sb.ToString();
    }
}
