using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Modules.Firm.Events;
using StoreAssistantPro.Modules.Firm.Services;

namespace StoreAssistantPro.Modules.Firm.ViewModels;

public partial class FirmViewModel : BaseViewModel
{
    private readonly IFirmService _firmService;
    private readonly IEventBus _eventBus;
    private const int TotalSteps = 3;

    public FirmViewModel(
        IFirmService firmService,
        IEventBus eventBus)
    {
        _firmService = firmService;
        _eventBus = eventBus;
        CurrentStep = 1;
        SelectedFYStartMonth = "April";
        SelectedCurrencySymbol = "\u20B9";
    }

    // ── Wizard navigation ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(IsLastStep))]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyPropertyChangedFor(nameof(StepIndicator))]
    public partial int CurrentStep { get; set; }

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool CanGoBack => CurrentStep > 1;
    public bool CanGoNext => CurrentStep < TotalSteps;
    public bool IsLastStep => CurrentStep == TotalSteps;
    public string StepIndicator => $"Step {CurrentStep} of {TotalSteps}";

    public string StepTitle => CurrentStep switch
    {
        1 => "Firm Profile",
        2 => "Tax & Legal",
        3 => "Regional Settings",
        _ => string.Empty
    };

    [RelayCommand]
    private void Next()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (!ValidateCurrentStep())
        {
            ErrorMessage = "Please fix the highlighted fields.";
            return;
        }

        if (CurrentStep < TotalSteps)
            CurrentStep++;
    }

    [RelayCommand]
    private void Back()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        if (CurrentStep > 1)
            CurrentStep--;
    }

    [RelayCommand]
    private Task ConfirmStepAsync()
    {
        if (CanGoNext)
        {
            Next();
            return Task.CompletedTask;
        }
        return SaveFirmAsync();
    }

    /// <summary>
    /// Validates only [Required] fields for the current step.
    /// Never clears optional field errors — their red borders must
    /// remain visible as real-time feedback while the user types.
    /// Only checks the specific Required field's errors, not global
    /// HasErrors (which would include optional field format errors).
    /// </summary>
    private bool ValidateCurrentStep()
    {
        ClearErrors();

        switch (CurrentStep)
        {
            case 1:
                ValidateProperty(FirmName, nameof(FirmName));
                return !HasErrorsFor(nameof(FirmName));
            // Steps 2 & 3 have no required fields — free navigation
        }

        return true;
    }

    private bool HasErrorsFor(string propertyName)
    {
        foreach (var _ in GetErrors(propertyName))
            return true;
        return false;
    }

    // ── Step 1: Firm Profile ──

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Firm name is required.")]
    [MaxLength(100, ErrorMessage = "Firm name cannot exceed 100 characters.")]
    public partial string FirmName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    public partial string Address { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(50, ErrorMessage = "State cannot exceed 50 characters.")]
    public partial string State { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [RegularExpression(@"^$|^\d{6}$", ErrorMessage = "Pincode must be exactly 6 digits.")]
    public partial string Pincode { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(15, ErrorMessage = "Phone cannot exceed 15 characters.")]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    [RegularExpression(@"^$|^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email address.")]
    public partial string Email { get; set; } = string.Empty;

    // ── Step 2: Tax & Legal ──

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(15, ErrorMessage = "GSTIN cannot exceed 15 characters.")]
    [RegularExpression(@"^$|^\d{2}[A-Z]{5}\d{4}[A-Z]\d[Z][A-Z\d]$", ErrorMessage = "Invalid GSTIN format.")]
    public partial string GSTNumber { get; set; } = string.Empty;

    partial void OnGSTNumberChanged(string value)
    {
        if (value.Length > 0 && value != value.ToUpperInvariant())
            GSTNumber = value.ToUpperInvariant();
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(10, ErrorMessage = "PAN cannot exceed 10 characters.")]
    [RegularExpression(@"^$|^[A-Z]{5}\d{4}[A-Z]$", ErrorMessage = "Invalid PAN format (e.g., ABCDE1234F).")]
    public partial string PANNumber { get; set; } = string.Empty;

    partial void OnPANNumberChanged(string value)
    {
        if (value.Length > 0 && value != value.ToUpperInvariant())
            PANNumber = value.ToUpperInvariant();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompositionScheme))]
    public partial string SelectedGstRegistrationType { get; set; } = "Regular";

    public ObservableCollection<string> GstRegistrationTypes { get; } = ["Regular", "Composition", "Unregistered"];

    public bool IsCompositionScheme => SelectedGstRegistrationType == "Composition";

    [ObservableProperty]
    public partial string CompositionRate { get; set; } = "1";

    // ── Step 3: Regional Settings ──

    [ObservableProperty]
    public partial string SelectedCurrencySymbol { get; set; }

    public ObservableCollection<string> CurrencySymbols { get; } =
    [
        "\u20B9",
        "Rs."
    ];

    public ObservableCollection<string> Months { get; } = new(
        Enumerable.Range(1, 12).Select(m => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m)));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinancialYearDisplay))]
    public partial string SelectedFYStartMonth { get; set; }

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

    [RelayCommand]
    private Task LoadFirmAsync() => RunLoadAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        var config = await _firmService.GetFirmAsync(ct);
        if (config is null) return;

        FirmName = config.FirmName;
        Address = config.Address;
        State = config.State;
        Pincode = config.Pincode;
        Phone = config.Phone;
        Email = config.Email;
        GSTNumber = config.GSTNumber ?? string.Empty;
        PANNumber = config.PANNumber ?? string.Empty;
        SelectedGstRegistrationType = config.GstRegistrationType ?? "Regular";
        CompositionRate = config.CompositionSchemeRate.ToString(CultureInfo.InvariantCulture);
        SelectedCurrencySymbol = config.CurrencySymbol;
        SelectedFYStartMonth = MonthIndexToName(config.FinancialYearStartMonth);
        SelectedDateFormat = config.DateFormat;
    });

    [RelayCommand]
    private Task SaveFirmAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        ValidateAllProperties();
        if (HasErrors)
            return;

        var trimmedName = FirmName.Trim();
        var fyStartMonth = MonthNameToIndex(SelectedFYStartMonth);
        var fyEndMonth = (fyStartMonth + 10) % 12 + 1;

        var dto = new FirmUpdateDto(
            FirmName: trimmedName,
            Address: Address.Trim(),
            State: State.Trim(),
            Pincode: Pincode.Trim(),
            Phone: Phone.Trim(),
            Email: Email.Trim(),
            GSTNumber: GSTNumber.Trim(),
            PANNumber: PANNumber.Trim(),
            GstRegistrationType: SelectedGstRegistrationType,
            CompositionSchemeRate: decimal.TryParse(CompositionRate, out var rate) ? rate : 1.0m,
            StateCode: GetStateCodeFromGstOrState(),
            FinancialYearStartMonth: fyStartMonth,
            FinancialYearEndMonth: fyEndMonth,
            CurrencySymbol: SelectedCurrencySymbol,
            DateFormat: SelectedDateFormat,
            NumberFormat: "Indian");

        await _firmService.UpdateFirmAsync(dto, ct);
        await _eventBus.PublishAsync(new FirmUpdatedEvent(trimmedName, SelectedCurrencySymbol, SelectedDateFormat));
        SuccessMessage = "Firm information saved.";
    });

    private static string MonthIndexToName(int month) =>
        CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(Math.Clamp(month, 1, 12));

    private static int MonthNameToIndex(string name)
    {
        for (var i = 1; i <= 12; i++)
            if (CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(i) == name)
                return i;
        return 4;
    }

    private string? GetStateCodeFromGstOrState()
    {
        if (!string.IsNullOrWhiteSpace(GSTNumber) && GSTNumber.Length >= 2)
        {
            var code = GSTNumber[..2];
            if (code.All(char.IsDigit))
                return code;
        }
        return GetStateCodeFromName(State.Trim());
    }

    private static string? GetStateCodeFromName(string stateName) => stateName switch
    {
        "Jammu & Kashmir" => "01", "Himachal Pradesh" => "02", "Punjab" => "03",
        "Chandigarh" => "04", "Uttarakhand" => "05", "Haryana" => "06",
        "Delhi" => "07", "Rajasthan" => "08", "Uttar Pradesh" => "09",
        "Bihar" => "10", "Sikkim" => "11", "Arunachal Pradesh" => "12",
        "Nagaland" => "13", "Manipur" => "14", "Mizoram" => "15",
        "Tripura" => "16", "Meghalaya" => "17", "Assam" => "18",
        "West Bengal" => "19", "Jharkhand" => "20", "Odisha" => "21",
        "Chhattisgarh" => "22", "Madhya Pradesh" => "23", "Gujarat" => "24",
        "Dadra & Nagar Haveli" => "26", "Maharashtra" => "27", "Karnataka" => "29",
        "Goa" => "30", "Lakshadweep" => "31", "Kerala" => "32",
        "Tamil Nadu" => "33", "Puducherry" => "34", "Andaman & Nicobar" => "35",
        "Telangana" => "36", "Andhra Pradesh" => "37", "Ladakh" => "38",
        _ => null
    };
}
