using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Modules.Firm.Events;
using StoreAssistantPro.Modules.Firm.Services;

namespace StoreAssistantPro.Modules.Firm.ViewModels;

public partial class FirmViewModel : BaseViewModel
{
    private static readonly HashSet<string> DirtyTrackedProperties =
    [
        nameof(FirmName),
        nameof(Address),
        nameof(State),
        nameof(Pincode),
        nameof(Phone),
        nameof(Email),
        nameof(GSTNumber),
        nameof(PANNumber),
        nameof(SelectedGstRegistrationType),
        nameof(CompositionRate),
        nameof(SelectedCurrencySymbol),
        nameof(SelectedFYStartMonth),
        nameof(SelectedDateFormat),
        nameof(SelectedTaxMode),
        nameof(SelectedRoundingMethod),
        nameof(SelectedNumberToWordsLanguage),
        nameof(NegativeStockAllowed),
        nameof(InvoicePrefix),
        nameof(ReceiptFooterText),
        nameof(LogoPath),
        nameof(BankName),
        nameof(BankAccountNumber),
        nameof(BankIFSC),
        nameof(ReceiptHeaderText),
        nameof(SelectedInvoiceResetPeriod)
    ];

    private static readonly string[] ValidationFieldOrder =
    [
        nameof(FirmName),
        nameof(Address),
        nameof(State),
        nameof(Pincode),
        nameof(Phone),
        nameof(Email),
        nameof(GSTNumber),
        nameof(PANNumber),
        nameof(CompositionRate),
        nameof(SelectedTaxMode),
        nameof(SelectedRoundingMethod),
        nameof(SelectedNumberToWordsLanguage),
        nameof(SelectedFYStartMonth),
        nameof(SelectedDateFormat),
        nameof(SelectedCurrencySymbol),
        nameof(InvoicePrefix),
        nameof(ReceiptFooterText)
    ];

    private readonly IFirmService _firmService;
    private readonly IEventBus _eventBus;
    private bool _isHydrating;

    public FirmViewModel(
        IFirmService firmService,
        IEventBus eventBus)
    {
        _firmService = firmService;
        _eventBus = eventBus;

        SelectedFYStartMonth = "April";
        SelectedCurrencySymbol = "\u20B9";
        InvoicePrefix = "INV";
        ReceiptFooterText = "Thank you! Visit again!";
        LogoPath = string.Empty;
        BankName = string.Empty;
        BankAccountNumber = string.Empty;
        BankIFSC = string.Empty;
        ReceiptHeaderText = string.Empty;
        SelectedInvoiceResetPeriod = "Never";
        PropertyChanged += OnFirmPropertyChanged;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DirtyStateSummaryText))]
    public partial bool IsDirty { get; set; }

    public string DirtyStateSummaryText => IsDirty
        ? "You have unsaved business settings changes."
        : "No unsaved business settings changes.";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Firm name is required.")]
    [MaxLength(100, ErrorMessage = "Firm name cannot exceed 100 characters.")]
    public partial string FirmName { get; set; } = string.Empty;

    partial void OnFirmNameChanged(string value) => OnEditableFieldChanged();

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    public partial string Address { get; set; } = string.Empty;

    partial void OnAddressChanged(string value) => OnEditableFieldChanged();

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(50, ErrorMessage = "State cannot exceed 50 characters.")]
    [CustomValidation(typeof(BusinessProfileRules), nameof(BusinessProfileRules.ValidateIndianState))]
    [NotifyPropertyChangedFor(nameof(StateValidationHint))]
    [NotifyPropertyChangedFor(nameof(GstinValidationHint))]
    public partial string State { get; set; } = string.Empty;

    partial void OnStateChanged(string value)
    {
        OnEditableFieldChanged();
        ValidateProperty(State, nameof(State));
        ValidateProperty(GSTNumber, nameof(GSTNumber));
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [RegularExpression(@"^$|^\d{6}$", ErrorMessage = "Pincode must be exactly 6 digits.")]
    public partial string Pincode { get; set; } = string.Empty;

    partial void OnPincodeChanged(string value) => OnEditableFieldChanged();

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [RegularExpression(@"^$|^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits.")]
    [NotifyPropertyChangedFor(nameof(PhoneValidationHint))]
    public partial string Phone { get; set; } = string.Empty;

    partial void OnPhoneChanged(string value) => OnEditableFieldChanged();

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    [RegularExpression(@"^$|^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email address.")]
    public partial string Email { get; set; } = string.Empty;

    partial void OnEmailChanged(string value) => OnEditableFieldChanged();

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(15, ErrorMessage = "GSTIN cannot exceed 15 characters.")]
    [CustomValidation(typeof(BusinessProfileRules), nameof(BusinessProfileRules.ValidateGstin))]
    [NotifyPropertyChangedFor(nameof(GstinValidationHint))]
    public partial string GSTNumber { get; set; } = string.Empty;

    partial void OnGSTNumberChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value != value.ToUpperInvariant())
        {
            GSTNumber = value.ToUpperInvariant();
            return;
        }

        OnEditableFieldChanged();
        ValidateProperty(GSTNumber, nameof(GSTNumber));
        ValidateProperty(State, nameof(State));
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(10, ErrorMessage = "PAN cannot exceed 10 characters.")]
    [CustomValidation(typeof(BusinessProfileRules), nameof(BusinessProfileRules.ValidatePan))]
    [NotifyPropertyChangedFor(nameof(PanValidationHint))]
    public partial string PANNumber { get; set; } = string.Empty;

    partial void OnPANNumberChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value != value.ToUpperInvariant())
        {
            PANNumber = value.ToUpperInvariant();
            return;
        }

        OnEditableFieldChanged();
        ValidateProperty(PANNumber, nameof(PANNumber));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompositionScheme))]
    [NotifyPropertyChangedFor(nameof(CompositionRateValidationHint))]
    public partial string SelectedGstRegistrationType { get; set; } = "Regular";

    partial void OnSelectedGstRegistrationTypeChanged(string value)
    {
        OnEditableFieldChanged();
        ValidateProperty(CompositionRate, nameof(CompositionRate));
    }

    public ObservableCollection<string> GstRegistrationTypes { get; } = ["Regular", "Composition", "Unregistered"];

    public bool IsCompositionScheme => SelectedGstRegistrationType == "Composition";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(BusinessProfileRules), nameof(BusinessProfileRules.ValidateCompositionRate))]
    [NotifyPropertyChangedFor(nameof(CompositionRateValidationHint))]
    public partial string CompositionRate { get; set; } = "1";

    partial void OnCompositionRateChanged(string value)
    {
        OnEditableFieldChanged();
        ValidateProperty(CompositionRate, nameof(CompositionRate));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrencyPreview))]
    public partial string SelectedCurrencySymbol { get; set; }

    partial void OnSelectedCurrencySymbolChanged(string value) => OnEditableFieldChanged();

    public ObservableCollection<string> CurrencySymbols { get; } = ["\u20B9", "Rs."];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinancialYearDisplay))]
    public partial string SelectedFYStartMonth { get; set; }

    partial void OnSelectedFYStartMonthChanged(string value) => OnEditableFieldChanged();

    public ObservableCollection<string> Months { get; } = new(
        Enumerable.Range(1, 12).Select(month => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateFormatPreview))]
    public partial string SelectedDateFormat { get; set; } = "dd/MM/yyyy";

    partial void OnSelectedDateFormatChanged(string value) => OnEditableFieldChanged();

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TaxModeHint))]
    public partial string SelectedTaxMode { get; set; } = "Tax-Exclusive";

    partial void OnSelectedTaxModeChanged(string value) => OnEditableFieldChanged();

    public ObservableCollection<string> TaxModes { get; } = ["Tax-Inclusive (MRP)", "Tax-Exclusive"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoundingPreview))]
    public partial string SelectedRoundingMethod { get; set; } = "No Rounding";

    partial void OnSelectedRoundingMethodChanged(string value) => OnEditableFieldChanged();

    public ObservableCollection<string> RoundingMethods { get; } =
        ["No Rounding", "Round to nearest \u20B91", "Round to nearest \u20B95", "Round to nearest \u20B910"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NumberToWordsPreview))]
    public partial string SelectedNumberToWordsLanguage { get; set; } = "English";

    partial void OnSelectedNumberToWordsLanguageChanged(string value) => OnEditableFieldChanged();

    public ObservableCollection<string> NumberToWordsLanguages { get; } = ["English", "Hindi"];

    [ObservableProperty]
    public partial bool NegativeStockAllowed { get; set; }

    partial void OnNegativeStockAllowedChanged(bool value) => OnEditableFieldChanged();

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(20, ErrorMessage = "Invoice prefix cannot exceed 20 characters.")]
    public partial string InvoicePrefix { get; set; }

    partial void OnInvoicePrefixChanged(string value) => OnEditableFieldChanged();

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(200, ErrorMessage = "Receipt footer text cannot exceed 200 characters.")]
    public partial string ReceiptFooterText { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(500, ErrorMessage = "Logo path cannot exceed 500 characters.")]
    public partial string LogoPath { get; set; }

    partial void OnLogoPathChanged(string value) => OnEditableFieldChanged();

    [RelayCommand]
    private void BrowseLogo()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Firm Logo",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
            LogoPath = dialog.FileName;
    }

    [RelayCommand]
    private void ClearLogo() => LogoPath = string.Empty;

    // ?? Bank details (#312) ??

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(100, ErrorMessage = "Bank name cannot exceed 100 characters.")]
    public partial string BankName { get; set; }

    partial void OnBankNameChanged(string value) => OnEditableFieldChanged();

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(30, ErrorMessage = "Account number cannot exceed 30 characters.")]
    public partial string BankAccountNumber { get; set; }

    partial void OnBankAccountNumberChanged(string value) => OnEditableFieldChanged();

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(11, ErrorMessage = "IFSC code cannot exceed 11 characters.")]
    [RegularExpression(@"^$|^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "IFSC format: ABCD0123456")]
    public partial string BankIFSC { get; set; }

    partial void OnBankIFSCChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value != value.ToUpperInvariant())
        {
            BankIFSC = value.ToUpperInvariant();
            return;
        }
        OnEditableFieldChanged();
    }

    // ?? Receipt header text (#316) ??

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(200, ErrorMessage = "Receipt header text cannot exceed 200 characters.")]
    public partial string ReceiptHeaderText { get; set; }

    partial void OnReceiptHeaderTextChanged(string value) => OnEditableFieldChanged();

    // ?? Invoice reset period (#314) ??

    [ObservableProperty]
    public partial string SelectedInvoiceResetPeriod { get; set; }

    partial void OnSelectedInvoiceResetPeriodChanged(string value) => OnEditableFieldChanged();

    public ObservableCollection<string> InvoiceResetPeriods { get; } = ["Never", "Monthly", "Annually"];

    public ObservableCollection<string> IndianStates { get; } = new(BusinessProfileRules.IndianStateNames);

    public string FinancialYearDisplay
    {
        get
        {
            var startIndex = Months.IndexOf(SelectedFYStartMonth);
            if (startIndex < 0)
                return "April - March";

            var endIndex = (startIndex + 11) % 12;
            return $"{Months[startIndex]} - {Months[endIndex]}";
        }
    }

    public string CurrencyPreview => $"Invoice preview: {SelectedCurrencySymbol} 1,00,000.00";

    public string DateFormatPreview
    {
        get
        {
            try
            {
                return $"e.g. {DateTime.Today.ToString(SelectedDateFormat, CultureInfo.InvariantCulture)}";
            }
            catch (FormatException)
            {
                return string.Empty;
            }
        }
    }

    public override string WorkingMessage => IsLoading
        ? "Loading business settings..."
        : "Saving business settings...";

    public string TaxModeHint => SelectedTaxMode == "Tax-Inclusive (MRP)"
        ? "Prices already include GST. Tax is back-calculated from the final amount."
        : "GST is added on top of the base selling price.";

    public string RoundingPreview => SelectedRoundingMethod switch
    {
        "Round to nearest \u20B91" => "Example: \u20B9 1,499.50 -> \u20B9 1,500",
        "Round to nearest \u20B95" => "Example: \u20B9 1,497 -> \u20B9 1,495",
        "Round to nearest \u20B910" => "Example: \u20B9 1,493 -> \u20B9 1,490",
        _ => "No rounding will be applied to the invoice total."
    };

    public string NumberToWordsPreview => SelectedNumberToWordsLanguage == "Hindi"
        ? "Example: \u090F\u0915 \u0932\u093E\u0916 \u0930\u0941\u092A\u092F\u0947"
        : "Example: One Lakh Rupees";

    public string CompositionRateValidationHint
    {
        get
        {
            if (!IsCompositionScheme)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(CompositionRate))
                return "Enter the composition rate applied to turnover.";

            if (!BusinessProfileRules.TryParseCompositionRate(CompositionRate, out _))
                return "Must be a number (e.g. 1, 1.5, 6)";

            return BusinessProfileRules.IsValidCompositionRate(CompositionRate)
                ? "\u2713"
                : "Must be between 0 and 100";
        }
    }

    public string PhoneValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Phone))
                return string.Empty;

            var normalized = Phone.Trim();
            return BusinessProfileRules.IsValidPhone(normalized)
                ? $"\u2713 {normalized[..5]} {normalized[5..]}"
                : "Enter a 10-digit phone number";
        }
    }

    public string StateValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(State))
                return string.Empty;

            if (!BusinessProfileRules.IsValidIndianState(State))
                return "Select a valid Indian state from the list";

            return BusinessProfileRules.IsGstinStateConsistent(GSTNumber, State)
                ? "\u2713"
                : "GSTIN state code does not match selected state";
        }
    }

    public string GstinValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(GSTNumber))
                return string.Empty;

            var gstin = GSTNumber.Trim().ToUpperInvariant();
            if (gstin.Length < 15 && gstin.Length > 0)
                return "Format: 27ABCDE1234F1Z5";

            var stateCode = BusinessProfileRules.ExtractKnownGstinStateCode(gstin);
            var stateName = BusinessProfileRules.GetStateNameByCode(stateCode);
            var prefix = stateName is null ? "\u2713" : $"\u2713 State: {stateName}";

            if (!BusinessProfileRules.IsValidGstin(gstin))
            {
                return gstin.Length == 15 && SetupStyleChecksumFailure(gstin)
                    ? $"{prefix} - check digit mismatch"
                    : "Format: 27ABCDE1234F1Z5";
            }

            if (!BusinessProfileRules.IsGstinStateConsistent(gstin, State))
                return $"{prefix} - differs from selected state ({State.Trim()})";

            return prefix;
        }
    }

    public string PanValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PANNumber))
                return string.Empty;

            if (!BusinessProfileRules.IsValidPan(PANNumber))
                return "Format: ABCDE1234F";

            var entityType = PANNumber.Length >= 4 ? GetPanEntityType(PANNumber[3]) : null;
            return entityType is null ? "\u2713" : $"\u2713 {entityType}";
        }
    }

    [RelayCommand]
    private Task LoadFirmAsync() => RunLoadAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        var snapshot = await _firmService.GetFirmAsync(ct).ConfigureAwait(false);
        if (snapshot is null)
            return;

        _isHydrating = true;
        try
        {
            FirmName = snapshot.FirmName;
            Address = snapshot.Address;
            State = BusinessProfileRules.GetCanonicalStateName(snapshot.State)
                ?? BusinessProfileRules.GetStateNameByCode(snapshot.StateCode)
                ?? string.Empty;
            Pincode = snapshot.Pincode;
            Phone = BusinessProfileRules.IsValidPhone(snapshot.Phone) ? snapshot.Phone : string.Empty;
            Email = snapshot.Email;
            GSTNumber = snapshot.GSTNumber ?? string.Empty;
            PANNumber = snapshot.PANNumber ?? string.Empty;
            SelectedGstRegistrationType = NormalizeGstRegistrationType(snapshot.GstRegistrationType);
            CompositionRate = snapshot.CompositionSchemeRate.ToString(CultureInfo.InvariantCulture);
            SelectedCurrencySymbol = CurrencySymbols.Contains(snapshot.CurrencySymbol) ? snapshot.CurrencySymbol : "\u20B9";
            SelectedFYStartMonth = MonthIndexToName(snapshot.FinancialYearStartMonth);
            SelectedDateFormat = DateFormats.Contains(snapshot.DateFormat) ? snapshot.DateFormat : "dd/MM/yyyy";
            SelectedTaxMode = MapStoredTaxModeToDisplay(snapshot.DefaultTaxMode);
            SelectedRoundingMethod = MapStoredRoundingToDisplay(snapshot.RoundingMethod);
            SelectedNumberToWordsLanguage = NumberToWordsLanguages.Contains(snapshot.NumberToWordsLanguage)
                ? snapshot.NumberToWordsLanguage
                : "English";
            NegativeStockAllowed = snapshot.NegativeStockAllowed;
            InvoicePrefix = snapshot.InvoicePrefix;
            ReceiptFooterText = snapshot.ReceiptFooterText;
            LogoPath = snapshot.LogoPath;
            BankName = snapshot.BankName;
            BankAccountNumber = snapshot.BankAccountNumber;
            BankIFSC = snapshot.BankIFSC;
            ReceiptHeaderText = snapshot.ReceiptHeaderText;
            SelectedInvoiceResetPeriod = InvoiceResetPeriods.Contains(snapshot.InvoiceResetPeriod)
                ? snapshot.InvoiceResetPeriod
                : "Never";
        }
        finally
        {
            _isHydrating = false;
        }

        ClearErrors();
        ValidationErrors = [];
        ValidateProperty(FirmName, nameof(FirmName));
        ValidateProperty(Address, nameof(Address));
        ValidateProperty(State, nameof(State));
        ValidateProperty(Pincode, nameof(Pincode));
        ValidateProperty(Phone, nameof(Phone));
        ValidateProperty(Email, nameof(Email));
        ValidateProperty(GSTNumber, nameof(GSTNumber));
        ValidateProperty(PANNumber, nameof(PANNumber));
        ValidateProperty(CompositionRate, nameof(CompositionRate));
        IsDirty = false;
        FirstErrorFieldKey = string.Empty;
    });

    [RelayCommand]
    private Task SaveFirmAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;
        ValidateAllProperties();

        var validSelections = Validate(v => v
            .Rule(TaxModes.Contains(SelectedTaxMode), "Select a valid billing tax mode.", nameof(SelectedTaxMode))
            .Rule(RoundingMethods.Contains(SelectedRoundingMethod), "Select a valid invoice rounding rule.", nameof(SelectedRoundingMethod))
            .Rule(NumberToWordsLanguages.Contains(SelectedNumberToWordsLanguage), "Select a valid number-to-words language.", nameof(SelectedNumberToWordsLanguage))
            .Rule(Months.Contains(SelectedFYStartMonth), "Select a valid financial year start month.", nameof(SelectedFYStartMonth))
            .Rule(DateFormats.Contains(SelectedDateFormat), "Select a valid date format.", nameof(SelectedDateFormat))
            .Rule(CurrencySymbols.Contains(SelectedCurrencySymbol), "Select a valid currency symbol.", nameof(SelectedCurrencySymbol)));

        if (HasErrors || !validSelections)
        {
            var selectionErrors = ValidationErrors;
            var combinedErrors = CollectObservableValidationErrors();
            combinedErrors.AddRange(selectionErrors);
            ValidationErrors = combinedErrors
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (string.IsNullOrWhiteSpace(FirstErrorFieldKey))
                FirstErrorFieldKey = GetFirstInvalidFieldKey();

            if (string.IsNullOrEmpty(ErrorMessage))
                ErrorMessage = "Review the highlighted business fields before saving.";
            return;
        }

        var trimmedName = FirmName.Trim();
        var fyStartMonth = MonthNameToIndex(SelectedFYStartMonth);
        var fyEndMonth = (fyStartMonth + 10) % 12 + 1;
        var compositionRate = IsCompositionScheme && BusinessProfileRules.TryParseCompositionRate(CompositionRate, out var rate)
            ? rate
            : 0m;

        var dto = new FirmUpdateDto(
            FirmName: trimmedName,
            Address: Address.Trim(),
            State: (BusinessProfileRules.GetCanonicalStateName(State) ?? string.Empty).Trim(),
            Pincode: Pincode.Trim(),
            Phone: Phone.Trim(),
            Email: Email.Trim(),
            GSTNumber: string.IsNullOrWhiteSpace(GSTNumber) ? null : GSTNumber.Trim().ToUpperInvariant(),
            PANNumber: string.IsNullOrWhiteSpace(PANNumber) ? null : PANNumber.Trim().ToUpperInvariant(),
            GstRegistrationType: SelectedGstRegistrationType,
            CompositionSchemeRate: compositionRate,
            StateCode: BusinessProfileRules.GetStateCodeFromGstinOrState(GSTNumber, State),
            FinancialYearStartMonth: fyStartMonth,
            FinancialYearEndMonth: fyEndMonth,
            CurrencySymbol: SelectedCurrencySymbol,
            DateFormat: SelectedDateFormat,
            NumberFormat: "Indian",
            DefaultTaxMode: MapDisplayTaxModeToStorage(SelectedTaxMode),
            RoundingMethod: MapDisplayRoundingToStorage(SelectedRoundingMethod),
            NegativeStockAllowed: NegativeStockAllowed,
            NumberToWordsLanguage: SelectedNumberToWordsLanguage,
            InvoicePrefix: InvoicePrefix.Trim(),
            ReceiptFooterText: ReceiptFooterText.Trim(),
            LogoPath: LogoPath.Trim(),
            BankName: BankName.Trim(),
            BankAccountNumber: BankAccountNumber.Trim(),
            BankIFSC: string.IsNullOrWhiteSpace(BankIFSC) ? string.Empty : BankIFSC.Trim().ToUpperInvariant(),
            ReceiptHeaderText: ReceiptHeaderText.Trim(),
            InvoiceResetPeriod: SelectedInvoiceResetPeriod);

        await _firmService.UpdateFirmAsync(dto, ct).ConfigureAwait(false);
        await _eventBus.PublishAsync(new FirmUpdatedEvent(trimmedName, SelectedCurrencySymbol, SelectedDateFormat)).ConfigureAwait(false);

        SuccessMessage = "Business settings saved.";
        IsDirty = false;
        FirstErrorFieldKey = string.Empty;
        ValidationErrors = [];
    });

    public override void Dispose()
    {
        PropertyChanged -= OnFirmPropertyChanged;
        base.Dispose();
    }

    private void OnFirmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isHydrating || string.IsNullOrWhiteSpace(e.PropertyName))
            return;

        if (DirtyTrackedProperties.Contains(e.PropertyName))
            IsDirty = true;
    }

    private void OnEditableFieldChanged()
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
            ErrorMessage = string.Empty;

        if (!string.IsNullOrEmpty(SuccessMessage))
            SuccessMessage = string.Empty;

        if (!string.IsNullOrEmpty(FirstErrorFieldKey))
            FirstErrorFieldKey = string.Empty;

        ValidationErrors = [];
    }

    private List<string> CollectObservableValidationErrors()
    {
        var errors = new List<string>();

        foreach (var propertyName in ValidationFieldOrder)
        {
            if (GetErrors(propertyName) is not IEnumerable<object> propertyErrors)
                continue;

            foreach (var error in propertyErrors)
            {
                if (error is not null)
                    errors.Add(error.ToString() ?? string.Empty);
            }
        }

        return errors;
    }

    private string GetFirstInvalidFieldKey()
    {
        foreach (var propertyName in ValidationFieldOrder)
        {
            if (GetErrors(propertyName)?.Cast<object>().Any() == true)
                return propertyName;
        }

        return string.Empty;
    }

    private static string NormalizeGstRegistrationType(string? value) =>
        value is "Composition" or "Unregistered" ? value : "Regular";

    private static string MonthIndexToName(int month) =>
        CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(Math.Clamp(month, 1, 12));

    private static int MonthNameToIndex(string name)
    {
        for (var i = 1; i <= 12; i++)
        {
            if (CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(i) == name)
                return i;
        }

        return 4;
    }

    private static string MapStoredTaxModeToDisplay(string? storedValue) =>
        string.Equals(storedValue, "Inclusive", StringComparison.OrdinalIgnoreCase)
            ? "Tax-Inclusive (MRP)"
            : "Tax-Exclusive";

    private static string MapDisplayTaxModeToStorage(string displayValue) =>
        displayValue == "Tax-Inclusive (MRP)" ? "Inclusive" : "Exclusive";

    private static string MapStoredRoundingToDisplay(string? storedValue) => storedValue switch
    {
        "NearestOne" => "Round to nearest \u20B91",
        "NearestFive" => "Round to nearest \u20B95",
        "NearestTen" => "Round to nearest \u20B910",
        _ => "No Rounding"
    };

    private static string MapDisplayRoundingToStorage(string displayValue) => displayValue switch
    {
        "Round to nearest \u20B91" => "NearestOne",
        "Round to nearest \u20B95" => "NearestFive",
        "Round to nearest \u20B910" => "NearestTen",
        _ => "None"
    };

    private static bool SetupStyleChecksumFailure(string gstin) =>
        GstinRegex().IsMatch(gstin) && !BusinessProfileRules.VerifyGstinChecksum(gstin);

    private static string? GetPanEntityType(char code) => char.ToUpperInvariant(code) switch
    {
        'P' => "Individual",
        'C' => "Company",
        'H' => "HUF",
        'F' => "Firm",
        'A' => "AOP",
        'T' => "Trust",
        'B' => "BOI",
        'L' => "Local authority",
        'J' => "Artificial juridical person",
        'G' => "Government",
        _ => null
    };

    [GeneratedRegex(@"^\d{2}[A-Z]{5}\d{4}[A-Z]\d[A-Z][A-Z\d]$")]
    private static partial Regex GstinRegex();
}
