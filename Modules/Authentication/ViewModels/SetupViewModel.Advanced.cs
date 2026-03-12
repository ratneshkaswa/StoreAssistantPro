using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Modules.Authentication.ViewModels;

public partial class SetupViewModel
{
    // -- GST Registration --

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompositionScheme))]
    [NotifyPropertyChangedFor(nameof(CompositionRateHint))]
    [NotifyPropertyChangedFor(nameof(CompositionRateValidationHint))]
    [NotifyPropertyChangedFor(nameof(IsTaxSectionComplete))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string SelectedGstRegistrationType { get; set; } = "Unregistered";

    public ObservableCollection<string> GstRegistrationTypes { get; } = ["Regular", "Composition", "Unregistered"];

    public bool IsCompositionScheme => SelectedGstRegistrationType == "Composition";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompositionRateHint))]
    [NotifyPropertyChangedFor(nameof(CompositionRateValidationHint))]
    [NotifyPropertyChangedFor(nameof(IsTaxSectionComplete))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string CompositionRate { get; set; } = "1";

    partial void OnCompositionRateChanged(string value) => ClearErrorOnEdit();

    public string CompositionRateHint => IsCompositionScheme
        ? $"Flat {CompositionRate}% on turnover - no CGST/SGST breakup"
        : string.Empty;

    public string CompositionRateValidationHint
    {
        get
        {
            if (!IsCompositionScheme || string.IsNullOrWhiteSpace(CompositionRate)) return string.Empty;
            if (!TryParseRate(CompositionRate, out _))
                return "Must be a number (e.g. 1, 1.5, 6)";
            if (!IsValidCompositionRate(CompositionRate))
                return "Must be between 0 and 100";
            return string.Empty;
        }
    }

    // -- State Code (auto-derived) --

    public string DerivedStateCode
    {
        get
        {
            return BusinessProfileRules.GetStateCodeFromGstinOrState(GSTIN, State) ?? string.Empty;
        }
    }

    public string DerivedStateCodeDisplay
    {
        get
        {
            var code = DerivedStateCode;
            if (string.IsNullOrEmpty(code)) return string.Empty;
            var name = GetGstinStateName(code);
            return name != null ? $"\u2713 State Code: {code} \u2014 {name}" : string.Empty;
        }
    }

    // -- Regional settings --

    /// <summary>Currency symbol is always ₹ - India-exclusive app.</summary>
    public string SelectedCurrencySymbol => "\u20b9";

    public ObservableCollection<string> Months { get; } = new(
        Enumerable.Range(1, 12).Select(m => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m)));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinancialYearDisplay))]
    [NotifyPropertyChangedFor(nameof(IsRegionalSectionComplete))]
    public partial string SelectedFYStartMonth { get; set; } = "April";

    public string FinancialYearDisplay
    {
        get
        {
            var startIndex = Months.IndexOf(SelectedFYStartMonth);
            if (startIndex < 0) return "April \u2013 March";
            var endIndex = (startIndex + 11) % 12;
            return $"{Months[startIndex]} \u2013 {Months[endIndex]}";
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateFormatPreview))]
    [NotifyPropertyChangedFor(nameof(IsRegionalSectionComplete))]
    public partial string SelectedDateFormat { get; set; } = "dd/MM/yyyy";

    public ObservableCollection<string> DateFormats { get; } =
    [
        "dd/MM/yyyy",
        "dd-MM-yyyy",
        "dd.MM.yyyy",
        "d MMM yyyy",
        "dd MMM yyyy",
        "MM/dd/yyyy",
        "yyyy-MM-dd",
        "yyyy/MM/dd"
    ];

    public string DateFormatPreview
    {
        get
        {
            try
            {
                var istNow = _regionalSettings.Now;
                return $"e.g. {istNow.ToString(SelectedDateFormat, CultureInfo.InvariantCulture)}";
            }
            catch (FormatException) { return string.Empty; }
        }
    }

    public string CurrencyPreview => "e.g. \u20b9 1,00,000";

    // -- Business preferences --

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TaxModeHint))]
    [NotifyPropertyChangedFor(nameof(IsSystemSectionComplete))]
    public partial string SelectedTaxMode { get; set; } = "Tax-Exclusive";

    public ObservableCollection<string> TaxModes { get; } = ["Tax-Inclusive (MRP)", "Tax-Exclusive"];

    public string TaxModeHint => SelectedTaxMode == "Tax-Inclusive (MRP)"
        ? "Prices include tax. Tax is back-calculated from MRP."
        : "Tax is added on top of the base price.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoundingPreview))]
    [NotifyPropertyChangedFor(nameof(IsSystemSectionComplete))]
    public partial string SelectedRoundingMethod { get; set; } = "No Rounding";

    public ObservableCollection<string> RoundingMethods { get; } =
        ["No Rounding", "Round to nearest \u20b91", "Round to nearest \u20b95", "Round to nearest \u20b910"];

    public string RoundingPreview => SelectedRoundingMethod switch
    {
        "Round to nearest \u20b91" => "e.g. \u20b91,499.50 \u2192 \u20b91,500",
        "Round to nearest \u20b95" => "e.g. \u20b91,497 \u2192 \u20b91,495",
        "Round to nearest \u20b910" => "e.g. \u20b91,493 \u2192 \u20b91,490",
        _ => string.Empty
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NumberToWordsPreview))]
    [NotifyPropertyChangedFor(nameof(IsSystemSectionComplete))]
    public partial string SelectedNumberToWordsLanguage { get; set; } = "English";

    public ObservableCollection<string> NumberToWordsLanguages { get; } = ["English", "Hindi"];

    public string NumberToWordsPreview => SelectedNumberToWordsLanguage == "Hindi"
        ? "e.g. \u090f\u0915 \u0932\u093e\u0916 \u0930\u0941\u092a\u092f\u0947"
        : "e.g. One Lakh Rupees";

    [ObservableProperty]
    public partial bool NegativeStockAllowed { get; set; }

    // -- Backup --

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBackupConfigVisible))]
    [NotifyPropertyChangedFor(nameof(BackupTimeValidationHint))]
    [NotifyPropertyChangedFor(nameof(IsBackupSectionComplete))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial bool AutoBackupEnabled { get; set; }

    public bool IsBackupConfigVisible => AutoBackupEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackupTimeValidationHint))]
    [NotifyPropertyChangedFor(nameof(IsBackupSectionComplete))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string BackupTime { get; set; } = "22:00";

    partial void OnBackupTimeChanged(string value) => ClearErrorOnEdit();

    public string BackupTimeValidationHint
    {
        get
        {
            if (!AutoBackupEnabled || string.IsNullOrWhiteSpace(BackupTime)) return string.Empty;
            return IsValidBackupTime(BackupTime)
                ? "\u2713"
                : "Format: HH:mm (e.g. 22:00)";
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBackupSectionComplete))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string BackupLocation { get; set; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "StoreAssistantPro", "Backups");

    partial void OnBackupLocationChanged(string value) => ClearErrorOnEdit();

    // -- Advanced section completion indicators --

    public bool IsTaxSectionComplete =>
        (!IsCompositionScheme || IsValidCompositionRate(CompositionRate))
        && (string.IsNullOrWhiteSpace(GSTIN) || IsValidGstin(GSTIN))
        && (string.IsNullOrWhiteSpace(PAN) || IsValidPan(PAN));

    public bool IsRegionalSectionComplete =>
        Months.Contains(SelectedFYStartMonth)
        && DateFormats.Contains(SelectedDateFormat);

    public bool IsBackupSectionComplete =>
        !AutoBackupEnabled ||
        (IsValidBackupTime(BackupTime) && !string.IsNullOrWhiteSpace(BackupLocation) && BackupFolderExists(BackupLocation));

    public bool IsSystemSectionComplete =>
        TaxModes.Contains(SelectedTaxMode)
        && RoundingMethods.Contains(SelectedRoundingMethod)
        && NumberToWordsLanguages.Contains(SelectedNumberToWordsLanguage);

    private static string? GetGstinStateName(string code) =>
        BusinessProfileRules.GetStateNameByCode(code);

    private static string? GetStateCodeFromName(string stateName) =>
        BusinessProfileRules.GetStateCodeFromName(stateName);

    private static int MonthNameToIndex(string name)
    {
        for (var i = 1; i <= 12; i++)
            if (CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(i) == name)
                return i;
        return 4;
    }

    private static bool IsGstinStateConsistent(string gstinValue, string stateValue) =>
        BusinessProfileRules.IsGstinStateConsistent(gstinValue, stateValue);

    private static string? NormalizeBackupPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var trimmed = path.Trim().Trim('"');
        if (trimmed.Length == 0)
            return null;

        var root = System.IO.Path.GetPathRoot(trimmed);
        var normalized = trimmed.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

        if (!string.IsNullOrWhiteSpace(root))
        {
            var normalizedRoot = root.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            if (string.Equals(normalized, normalizedRoot, StringComparison.OrdinalIgnoreCase))
                return root;
        }

        return normalized;
    }

    private static bool IsValidBackupLocationPath(string value)
    {
        var normalized = NormalizeBackupPath(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        if (!System.IO.Path.IsPathRooted(normalized))
            return false;

        return normalized.IndexOfAny(System.IO.Path.GetInvalidPathChars()) < 0;
    }

    private static bool BackupFolderExists(string value)
    {
        var normalized = NormalizeBackupPath(value);
        return !string.IsNullOrWhiteSpace(normalized) && System.IO.Directory.Exists(normalized);
    }

    private static bool TryParseRate(string value, out decimal rate) =>
        BusinessProfileRules.TryParseCompositionRate(value, out rate);

    private static bool IsValidCompositionRate(string value) =>
        BusinessProfileRules.IsValidCompositionRate(value);

    private static bool IsValidBackupTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return TimeOnly.TryParseExact(value.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static bool IsValidGstin(string value) =>
        BusinessProfileRules.IsValidGstin(value);

    private static bool IsValidPan(string value) =>
        BusinessProfileRules.IsValidPan(value);
}
