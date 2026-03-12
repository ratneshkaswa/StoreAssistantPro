using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Authentication.Commands;

namespace StoreAssistantPro.Modules.Authentication.ViewModels;

public partial class SetupViewModel : BaseViewModel
{
    private static readonly TimeSpan DraftAutoSaveInterval = TimeSpan.FromSeconds(5);
    private static readonly byte[] DraftEntropy = Encoding.UTF8.GetBytes("StoreAssistantPro.SetupDraft.v1");
    private static readonly string DraftFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StoreAssistantPro",
        "setup-draft.dat");

    private static readonly (string Code, string Name)[] IndianStateData =
        BusinessProfileRules.IndianStateData.ToArray();

    private static readonly Dictionary<string, string> IndianStateNameByCode =
        new(BusinessProfileRules.IndianStateNameByCode, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> IndianStateCodeByName =
        new(BusinessProfileRules.IndianStateCodeByName, StringComparer.OrdinalIgnoreCase);

    // -- Firm details --

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsFirmSectionComplete))]
    [NotifyPropertyChangedFor(nameof(FirmSectionStatusText))]
    public partial string FirmName { get; set; } = string.Empty;

    partial void OnFirmNameChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsFirmSectionComplete))]
    [NotifyPropertyChangedFor(nameof(FirmSectionStatusText))]
    public partial string Address { get; set; } = string.Empty;

    partial void OnAddressChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DerivedStateCode))]
    [NotifyPropertyChangedFor(nameof(DerivedStateCodeDisplay))]
    [NotifyPropertyChangedFor(nameof(GstinValidationHint))]
    [NotifyPropertyChangedFor(nameof(StateValidationHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsFirmSectionComplete))]
    [NotifyPropertyChangedFor(nameof(FirmSectionStatusText))]
    public partial string State { get; set; } = string.Empty;

    partial void OnStateChanged(string value) => ClearErrorOnEdit();

    public ObservableCollection<string> IndianStates { get; } =
        new(BusinessProfileRules.IndianStateNames);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PincodeValidationHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsFirmSectionComplete))]
    [NotifyPropertyChangedFor(nameof(FirmSectionStatusText))]
    public partial string Pincode { get; set; } = string.Empty;

    partial void OnPincodeChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhoneValidationHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsFirmSectionComplete))]
    [NotifyPropertyChangedFor(nameof(FirmSectionStatusText))]
    public partial string Phone { get; set; } = string.Empty;

    partial void OnPhoneChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GstinValidationHint))]
    [NotifyPropertyChangedFor(nameof(GstinPanCrossHint))]
    [NotifyPropertyChangedFor(nameof(DerivedStateCode))]
    [NotifyPropertyChangedFor(nameof(DerivedStateCodeDisplay))]
    [NotifyPropertyChangedFor(nameof(StateValidationHint))]
    [NotifyPropertyChangedFor(nameof(IsTaxSectionComplete))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string GSTIN { get; set; } = string.Empty;

    partial void OnGSTINChanged(string value)
    {
        ClearErrorOnEdit();
        // Auto-fill State only when a complete GSTIN (15 chars) is entered
        if (value.Length == 15)
        {
            var stateName = GetGstinStateName(value[..2]);
            if (stateName != null && string.IsNullOrWhiteSpace(State))
                State = stateName;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PanValidationHint))]
    [NotifyPropertyChangedFor(nameof(GstinPanCrossHint))]
    [NotifyPropertyChangedFor(nameof(IsTaxSectionComplete))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string PAN { get; set; } = string.Empty;

    partial void OnPANChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmailValidationHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsFirmSectionComplete))]
    [NotifyPropertyChangedFor(nameof(FirmSectionStatusText))]
    public partial string Email { get; set; } = string.Empty;

    partial void OnEmailChanged(string value) => ClearErrorOnEdit();

    /// <summary>Currency code is always INR - India-exclusive app.</summary>
    public string CurrencyCode => "INR";


    // -- Live field validation hints --

    public string PhoneValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Phone)) return string.Empty;
            var normalized = Phone.Trim();
            if (!BusinessProfileRules.IsValidPhone(normalized)) return "Enter a 10-digit phone number";
            return $"\u2713 {normalized[..5]} {normalized[5..]}";
        }
    }

    public string EmailValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Email)) return string.Empty;
            return EmailRegex().IsMatch(Email) ? "\u2713" : "Enter a valid email address";
        }
    }

    public string GstinValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(GSTIN)) return string.Empty;
            var gstin = GSTIN.Trim().ToUpperInvariant();
            if (!GstinRegex().IsMatch(gstin)) return "Format: 22AAAAA0000A1Z5";
            var stateCode = gstin[..2];
            var stateName = GetGstinStateName(stateCode);
            var prefix = stateName != null ? $"\u2713 State: {stateName}" : "\u2713";
            var warnings = new System.Text.StringBuilder(prefix);
            if (stateName != null && !string.IsNullOrWhiteSpace(State)
                && !string.Equals(State, stateName, StringComparison.OrdinalIgnoreCase))
                warnings.Append($" \u2014 \u26a0 differs from selected state ({State})");
            if (gstin.Length == 15 && !VerifyGstinChecksum(gstin))
                warnings.Append(" \u2014 check digit mismatch");
            return warnings.ToString();
        }
    }

    /// <summary>
    /// Verifies the GSTIN check digit (15th character) per the official algorithm.
    /// </summary>
    public static bool VerifyGstinChecksum(string gstin) =>
        BusinessProfileRules.VerifyGstinChecksum(gstin);

    public string PanValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PAN)) return string.Empty;
            if (!PanRegex().IsMatch(PAN)) return "Format: ABCDE1234F";
            var entityType = PAN.Length >= 4 ? GetPanEntityType(PAN[3]) : null;
            return entityType != null ? $"\u2713 {entityType}" : "\u2713";
        }
    }

    private static string? GetPanEntityType(char code) => char.ToUpperInvariant(code) switch
    {
        'P' => "Individual",
        'C' => "Company",
        'H' => "HUF",
        'F' => "Firm",
        'A' => "AOP",
        'T' => "Trust",
        'B' => "BOI",
        'L' => "Local Authority",
        'J' => "Artificial Juridical Person",
        'G' => "Government",
        _ => null
    };

    public string PincodeValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Pincode)) return string.Empty;
            var pincode = Pincode.Trim();
            return pincode.Length == 6 && pincode.AsSpan().IndexOfAnyExceptInRange('0', '9') < 0
                ? "\u2713" : "Must be exactly 6 digits";
        }
    }

    public string StateValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(State))
                return string.Empty;

            var trimmed = State.Trim();
            if (!BusinessProfileRules.IsValidIndianState(trimmed))
                return "Select a valid Indian state from the list";

            if (!IsGstinStateConsistent(GSTIN, trimmed))
                return "GSTIN state code does not match selected state";

            return "\u2713";
        }
    }

    public string GstinPanCrossHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(GSTIN) || string.IsNullOrWhiteSpace(PAN)) return string.Empty;
            if (!GstinRegex().IsMatch(GSTIN) || !PanRegex().IsMatch(PAN)) return string.Empty;
            var panFromGstin = GSTIN[2..12];
            return string.Equals(panFromGstin, PAN, StringComparison.OrdinalIgnoreCase)
                ? "\u2713 PAN matches GSTIN"
                : "\u26a0 PAN does not match GSTIN characters 3\u201312";
        }
    }

    // -- PIN setup --

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdminPinWarning))]
    [NotifyPropertyChangedFor(nameof(AdminPinWarningDetail))]
    [NotifyPropertyChangedFor(nameof(AdminPinStrength))]
    [NotifyPropertyChangedFor(nameof(AdminPinStrengthText))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    [NotifyPropertyChangedFor(nameof(SecuritySectionStatusText))]
    public partial string AdminPin { get; set; } = string.Empty;

    partial void OnAdminPinChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdminConfirmHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    [NotifyPropertyChangedFor(nameof(SecuritySectionStatusText))]
    public partial string AdminPinConfirm { get; set; } = string.Empty;

    partial void OnAdminPinConfirmChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserPinWarning))]
    [NotifyPropertyChangedFor(nameof(UserPinWarningDetail))]
    [NotifyPropertyChangedFor(nameof(UserPinStrength))]
    [NotifyPropertyChangedFor(nameof(UserPinStrengthText))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    [NotifyPropertyChangedFor(nameof(SecuritySectionStatusText))]
    public partial string UserPin { get; set; } = string.Empty;

    partial void OnUserPinChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserConfirmHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    [NotifyPropertyChangedFor(nameof(SecuritySectionStatusText))]
    public partial string UserPinConfirm { get; set; } = string.Empty;

    partial void OnUserPinConfirmChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MasterConfirmHint))]
    [NotifyPropertyChangedFor(nameof(MasterPinWarning))]
    [NotifyPropertyChangedFor(nameof(MasterPinWarningDetail))]
    [NotifyPropertyChangedFor(nameof(MasterPinStrength))]
    [NotifyPropertyChangedFor(nameof(MasterPinStrengthText))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    [NotifyPropertyChangedFor(nameof(SecuritySectionStatusText))]
    public partial string MasterPin { get; set; } = string.Empty;

    partial void OnMasterPinChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MasterConfirmHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    [NotifyPropertyChangedFor(nameof(SecuritySectionStatusText))]
    public partial string MasterPinConfirm { get; set; } = string.Empty;

    partial void OnMasterPinConfirmChanged(string value) => ClearErrorOnEdit();

    // -- Weak PIN warnings (non-blocking guidance) --

    public string AdminPinWarning => GetPinWarning(AdminPin);
    public string AdminPinWarningDetail => GetPinWarningDetail(AdminPin);
    public string UserPinWarning => GetPinWarning(UserPin);
    public string UserPinWarningDetail => GetPinWarningDetail(UserPin);
    public string MasterPinWarning => GetMasterPinWarning(MasterPin);
    public string MasterPinWarningDetail => GetMasterPinWarningDetail(MasterPin);

    // S5: PIN strength (0-3: empty, weak, fair, strong)
    public int AdminPinStrength => GetPinStrength(AdminPin);
    public int UserPinStrength => GetPinStrength(UserPin);
    public int MasterPinStrength => GetMasterPinStrength(MasterPin);
    public string AdminPinStrengthText => GetPinStrengthText(AdminPinStrength);
    public string UserPinStrengthText => GetPinStrengthText(UserPinStrength);
    public string MasterPinStrengthText => GetPinStrengthText(MasterPinStrength);

    // Live confirm mismatch hints
    public string AdminConfirmHint => GetConfirmHint(AdminPin, AdminPinConfirm, 4);
    public string UserConfirmHint => GetConfirmHint(UserPin, UserPinConfirm, 4);
    public string MasterConfirmHint => GetConfirmHint(MasterPin, MasterPinConfirm, 6);

    // S7: Live cross-validation
    public string PinConflictWarning => GetPinConflictWarning();

    // -- Section completion indicators --

    public bool IsFirmSectionComplete =>
        !string.IsNullOrWhiteSpace(FirmName)
        && HasNoVisibleFirmDetailErrors();

    public string FirmSectionStatusText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FirmName))
                return "Required name missing";

            if (!HasNoVisibleFirmDetailErrors())
                return "Review highlighted details";

            return HasAnySupportingFirmDetail()
                ? "Ready"
                : "Required details complete";
        }
    }

    public bool IsSecuritySectionComplete =>
        InputValidator.IsValidUserPin(AdminPin) && AdminPin == AdminPinConfirm &&
        InputValidator.IsValidUserPin(UserPin) && UserPin == UserPinConfirm &&
        InputValidator.IsValidMasterPin(MasterPin) && MasterPin == MasterPinConfirm &&
        InputValidator.AreAllDistinct(AdminPin, UserPin) &&
        !MasterPinContainsRolePin(MasterPin, AdminPin, UserPin);

    public string SecuritySectionStatusText =>
        IsSecuritySectionComplete
            ? "Ready"
            : $"{GetCompletedSecurityChecks()} of 4 checks";

    // -- Required fields progress --

    private const int RequiredChecksTotal = 5;

    private int GetCompletedRequiredChecks()
    {
        var done = 0;

        if (!string.IsNullOrWhiteSpace(FirmName)) done++;
        if (InputValidator.IsValidUserPin(AdminPin) && AdminPin == AdminPinConfirm) done++;
        if (InputValidator.IsValidUserPin(UserPin) && UserPin == UserPinConfirm) done++;
        if (InputValidator.IsValidMasterPin(MasterPin) && MasterPin == MasterPinConfirm) done++;
        if (InputValidator.AreAllDistinct(AdminPin, UserPin)
            && !MasterPinContainsRolePin(MasterPin, AdminPin, UserPin)) done++;

        return done;
    }

    private int GetCompletedSecurityChecks()
    {
        var done = 0;

        if (InputValidator.IsValidUserPin(AdminPin) && AdminPin == AdminPinConfirm) done++;
        if (InputValidator.IsValidUserPin(UserPin) && UserPin == UserPinConfirm) done++;
        if (InputValidator.IsValidMasterPin(MasterPin) && MasterPin == MasterPinConfirm) done++;
        if (InputValidator.AreAllDistinct(AdminPin, UserPin)
            && !MasterPinContainsRolePin(MasterPin, AdminPin, UserPin)) done++;

        return done;
    }

    public bool IsReadyForSave =>
        GetCompletedRequiredChecks() == RequiredChecksTotal
        && HasNoOptionalValidationErrors();

    public string RequiredFieldsProgress
    {
        get
        {
            var done = GetCompletedRequiredChecks();
            if (done < RequiredChecksTotal)
                return $"{done} / {RequiredChecksTotal} required items complete";

            return IsReadyForSave
                ? "\u2713 Ready to save"
                : "Required items complete - review highlighted optional details";
        }
    }

    public string SaveReadinessMessage
    {
        get
        {
            if (IsReadyForSave)
                return "Ready to save.";

            return SaveBlockingIssue;
        }
    }

    public string SaveBlockingIssue
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FirmName))
                return "Firm name is required.";
            if (!(InputValidator.IsValidUserPin(AdminPin) && AdminPin == AdminPinConfirm))
                return "Admin PIN must be 4 digits and confirmed.";
            if (!(InputValidator.IsValidUserPin(UserPin) && UserPin == UserPinConfirm))
                return "User PIN must be 4 digits and confirmed.";
            if (!(InputValidator.IsValidMasterPin(MasterPin) && MasterPin == MasterPinConfirm))
                return "Master PIN must be 6 digits and confirmed.";
            if (!InputValidator.AreAllDistinct(AdminPin, UserPin) || MasterPinContainsRolePin(MasterPin, AdminPin, UserPin))
                return "All PINs must be unique.";
            if (!HasNoOptionalValidationErrors())
                return "Correct optional details before saving.";
            return "Complete setup checks to continue.";
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial bool UseEssentialSetupValidationOnly { get; set; } = true;

    private bool ShouldValidateAdvancedSetupFields => !UseEssentialSetupValidationOnly;

    // S10: Setup complete state
    [ObservableProperty]
    public partial bool IsSetupComplete { get; set; }

    [ObservableProperty]
    public partial string RedirectCountdown { get; set; } = string.Empty;

    public Action<bool?>? RequestClose { get; set; }

    public SetupViewModel(ICommandBus commandBus, IRegionalSettingsService regionalSettings) : base()
    {
        _commandBus = commandBus;
        _regionalSettings = regionalSettings;
        PropertyChanged += OnSetupPropertyChanged;
        _draftAutoSaveTimer = new Timer(_ => AutoSaveDraftSafe(), null, DraftAutoSaveInterval, DraftAutoSaveInterval);
        RestoreDraftSafe();
        IsDirty = false;
    }

    private readonly ICommandBus _commandBus;
    private readonly IRegionalSettingsService _regionalSettings;
    private CancellationTokenSource? _redirectCts;
    private readonly Timer _draftAutoSaveTimer;
    private volatile bool _isRestoringDraft;
    private volatile bool _isPersistingDraft;

    [ObservableProperty]
    public partial bool IsDirty { get; set; }

    [ObservableProperty]
    public partial bool ShowRolePins { get; set; }

    [ObservableProperty]
    public partial bool ShowMasterPins { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OptionalFirmToggleText))]
    public partial bool ShowOptionalFirmFields { get; set; }

    [ObservableProperty]
    public partial bool HasRecoveredDraft { get; set; }

    private void OnSetupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ErrorMessage) or nameof(FirstErrorFieldKey))
            OnPropertyChanged(nameof(ShouldShowGlobalError));

        if (e.PropertyName is nameof(IsDirty) or nameof(ErrorMessage) or nameof(SuccessMessage)
            or nameof(FirstErrorFieldKey) or nameof(IsBusy) or nameof(IsSetupComplete)
            or nameof(RedirectCountdown)
            or nameof(IsReadyForSave) or nameof(SaveBlockingIssue) or nameof(SaveReadinessMessage)
            or nameof(ShouldShowGlobalError) or nameof(OptionalFirmToggleText))
            return;

        if (!_isRestoringDraft)
            IsDirty = true;

        OnPropertyChanged(nameof(IsReadyForSave));
        OnPropertyChanged(nameof(SaveBlockingIssue));
        OnPropertyChanged(nameof(SaveReadinessMessage));
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(FirmName), "Firm name is required.", "FirmName")
            .Rule(InputValidator.IsValidUserPin(AdminPin), "Admin PIN must be exactly 4 digits.", "AdminPin")
            .Rule(InputValidator.AreEqual(AdminPin, AdminPinConfirm), "Admin PIN confirmation does not match.", "AdminPinConfirm")
            .Rule(InputValidator.IsValidUserPin(UserPin), "User PIN must be exactly 4 digits.", "UserPin")
            .Rule(InputValidator.AreEqual(UserPin, UserPinConfirm), "User PIN confirmation does not match.", "UserPinConfirm")
            .Rule(InputValidator.AreAllDistinct(AdminPin, UserPin), "Each role must have a unique PIN.", "PinConflict")
            .Rule(InputValidator.IsValidMasterPin(MasterPin), "Master PIN must be exactly 6 digits.", "MasterPin")
            .Rule(InputValidator.AreEqual(MasterPin, MasterPinConfirm), "Master PIN confirmation does not match.", "MasterPinConfirm")
            .Rule(!MasterPinContainsRolePin(MasterPin, AdminPin, UserPin), "Master PIN must not contain any role PIN.", "MasterPinContains")
            .Rule(!ShouldValidateAdvancedSetupFields || !IsCompositionScheme || IsValidCompositionRate(CompositionRate), "Composition rate must be a valid number (0-100).", "CompositionRate")
            .Rule(!ShouldValidateAdvancedSetupFields || string.IsNullOrWhiteSpace(GSTIN) || GstinRegex().IsMatch(GSTIN.Trim().ToUpperInvariant()), "GSTIN format is invalid.", "GSTIN")
            .Rule(!ShouldValidateAdvancedSetupFields || string.IsNullOrWhiteSpace(GSTIN) || GSTIN.Trim().Length != 15 || VerifyGstinChecksum(GSTIN.Trim().ToUpperInvariant()), "GSTIN check digit is invalid.", "GSTINChecksum")
            .Rule(!ShouldValidateAdvancedSetupFields || string.IsNullOrWhiteSpace(PAN) || PanRegex().IsMatch(PAN.Trim().ToUpperInvariant()), "PAN format is invalid.", "PAN")
            .Rule(!ShouldValidateAdvancedSetupFields || IsGstinStateConsistent(GSTIN, State), "GSTIN state code does not match selected state.", "State")
            .Rule(string.IsNullOrWhiteSpace(Pincode) || (Pincode.Trim().Length == 6 && Pincode.Trim().AsSpan().IndexOfAnyExceptInRange('0', '9') < 0), "Pincode must be exactly 6 digits.", "Pincode")
            .Rule(string.IsNullOrWhiteSpace(Email) || EmailRegex().IsMatch(Email.Trim()), "Email format is invalid.", "Email")
            .Rule(BusinessProfileRules.IsValidIndianState(State), "Please select a valid Indian state from the list.", "State")
            .Rule(string.IsNullOrWhiteSpace(Phone) || BusinessProfileRules.IsValidPhone(Phone.Trim()), "Phone must be exactly 10 digits.", "Phone")
            .Rule(!ShouldValidateAdvancedSetupFields || !AutoBackupEnabled || IsValidBackupTime(BackupTime), "Backup time must be in HH:mm format (e.g. 22:00).", "BackupTime")
            .Rule(!ShouldValidateAdvancedSetupFields || !AutoBackupEnabled || !string.IsNullOrWhiteSpace(BackupLocation), "Backup location is required when auto backup is enabled.", "BackupLocation")
            .Rule(!ShouldValidateAdvancedSetupFields || !AutoBackupEnabled || string.IsNullOrWhiteSpace(BackupLocation) || IsValidBackupLocationPath(BackupLocation), "Backup location path is invalid.", "BackupLocation")
            .Rule(!ShouldValidateAdvancedSetupFields || !AutoBackupEnabled || string.IsNullOrWhiteSpace(BackupLocation) || BackupFolderExists(BackupLocation), "Backup folder does not exist. Please create it first or choose a different path.", "BackupLocation")))
            return;

        var fyStartMonth = MonthNameToIndex(SelectedFYStartMonth);
        var fyEndMonth = (fyStartMonth + 10) % 12 + 1;

        var backupPath = AutoBackupEnabled
            ? NormalizeBackupPath(BackupLocation)
            : null;

        var result = await _commandBus.SendAsync(new CompleteFirstSetupCommand(
            FirmName.Trim(), Address.Trim(), State.Trim(), Pincode.Trim(),
            Phone.Trim(), Email.Trim(), GSTIN.Trim().ToUpperInvariant(),
            PAN.Trim().ToUpperInvariant(), CurrencyCode,
            SelectedCurrencySymbol, fyStartMonth, fyEndMonth, SelectedDateFormat,
            AdminPin, UserPin, MasterPin,
            new SetupBusinessOptions(
                !UseEssentialSetupValidationOnly,
                SelectedGstRegistrationType,
                TryParseRate(CompositionRate, out var rate) ? rate : 1.0m,
                string.IsNullOrEmpty(DerivedStateCode) ? null : DerivedStateCode,
                SelectedTaxMode,
                SelectedRoundingMethod,
                SelectedNumberToWordsLanguage,
                NegativeStockAllowed,
                AutoBackupEnabled,
                AutoBackupEnabled ? BackupTime : null,
                backupPath)), ct);

        if (result.Succeeded)
        {
            IsSetupComplete = true;
            IsDirty = false;
            HasRecoveredDraft = false;
            DeleteDraftSafe();
            StartRedirectCountdown();
        }
        else
            ErrorMessage = result.ErrorMessage ?? "Setup failed.";
    });

    [RelayCommand]
    private void CancelSetup()
    {
        if (IsSetupComplete)
        {
            GoToLogin();
            return;
        }

        CancelRedirectCountdown();
        RequestClose?.Invoke(false);
    }

    [RelayCommand]
    private void GoToLogin()
    {
        CancelRedirectCountdown();
        ClearSensitivePins();
        RequestClose?.Invoke(true);
    }

    private void StartRedirectCountdown()
    {
        CancelRedirectCountdown();
        RedirectCountdown = string.Empty;
        _redirectCts = new CancellationTokenSource();
        _ = RunRedirectCountdownAsync(_redirectCts.Token);
    }

    private async Task RunRedirectCountdownAsync(CancellationToken ct)
    {
        try
        {
            RedirectCountdown = "Opening login...";
            await Task.Delay(1200, ct);

            ClearSensitivePins();
            RequestClose?.Invoke(true);
        }
        catch (OperationCanceledException)
        {
            RedirectCountdown = string.Empty;
        }
    }

    private void CancelRedirectCountdown()
    {
        var cts = Interlocked.Exchange(ref _redirectCts, null);
        if (cts is null)
            return;

        cts.Cancel();
        cts.Dispose();
    }

    public void ClearSensitivePins()
    {
        AdminPin = string.Empty;
        AdminPinConfirm = string.Empty;
        UserPin = string.Empty;
        UserPinConfirm = string.Empty;
        MasterPin = string.Empty;
        MasterPinConfirm = string.Empty;
    }

    public bool ShouldShowGlobalError =>
        HasError && string.IsNullOrWhiteSpace(FirstErrorFieldKey);

    public string OptionalFirmToggleText =>
        ShowOptionalFirmFields ? "Less business details" : "More business details";

    private const string WeakPinShortText = "\u26a0 Weak PIN";
    private const string WeakPinDetailText = "\u26a0 Weak PIN \u2014 consider a less predictable combination.";
    private const string WeakMasterPinShortText = "\u26a0 Weak PIN";
    private const string WeakMasterPinDetailText = "\u26a0 Weak \u2014 consider a less predictable combination.";

    private static bool IsWeakUserPin(string pin) =>
        pin is "0000" or "1234" or "4321" or "1111" or "2222" or "3333"
            or "4444" or "5555" or "6666" or "7777" or "8888" or "9999"
            or "2345" or "3456" or "4567" or "5678" or "6789"
            or "9876" or "8765" or "7654" or "6543" or "5432";

    private static bool IsWeakMasterPin(string pin) =>
        pin is "000000" or "123456" or "654321" or "111111" or "222222" or "333333"
            or "444444" or "555555" or "666666" or "777777" or "888888" or "999999"
            or "234567" or "345678" or "456789" or "987654" or "876543" or "765432";

    private static string GetPinWarning(string pin) => pin.Length >= 4 && IsWeakUserPin(pin)
        ? WeakPinShortText
        : string.Empty;

    private static string GetPinWarningDetail(string pin) => pin.Length >= 4 && IsWeakUserPin(pin)
        ? WeakPinDetailText
        : string.Empty;

    // S5: PIN strength meter (0=empty, 1=weak, 2=fair, 3=strong)
    private static int GetPinStrength(string pin)
    {
        if (pin.Length < 4) return 0;
        if (IsWeakUserPin(pin))
            return 1;
        var distinct = pin.Distinct().Count();
        if (distinct <= 2) return 2;
        return 3;
    }

    private static string GetConfirmHint(string pin, string confirm, int expectedLength)
    {
        if (string.IsNullOrEmpty(confirm)) return string.Empty;
        if (confirm.Length < expectedLength) return string.Empty;
        return confirm == pin ? "\u2713 Match" : "\u2717 Does not match";
    }

    // S7: Live cross-validation
    private string GetPinConflictWarning()
    {
        var conflicts = new List<string>();

        if (AdminPin.Length == 4 && UserPin.Length == 4 && AdminPin == UserPin)
            conflicts.Add("Admin = User");

        // Master PIN must also be unique from role PINs
        if (MasterPin.Length == 6)
        {
            if (AdminPin.Length == 4 && MasterPin.Contains(AdminPin))
                conflicts.Add("Master contains Admin");
            if (UserPin.Length == 4 && MasterPin.Contains(UserPin))
                conflicts.Add("Master contains User");
        }

        return conflicts.Count > 0
            ? $"\u26a0 PIN conflicts: {string.Join(", ", conflicts)}"
            : string.Empty;
    }

    private static string GetMasterPinWarning(string pin)
    {
        return pin.Length >= 6 && IsWeakMasterPin(pin)
            ? WeakMasterPinShortText
            : string.Empty;
    }

    private static string GetMasterPinWarningDetail(string pin) => pin.Length >= 6 && IsWeakMasterPin(pin)
        ? WeakMasterPinDetailText
        : string.Empty;

    private static int GetMasterPinStrength(string pin)
    {
        if (pin.Length < 6) return 0;
        if (IsWeakMasterPin(pin))
            return 1;
        var distinct = pin.Distinct().Count();
        if (distinct <= 2) return 2;
        return 3;
    }

    private static string GetPinStrengthText(int strength) => strength switch
    {
        0 => "Not set",
        1 => "Weak",
        2 => "Good",
        _ => "Strong"
    };

    [GeneratedRegex(@"^\d{10}$")]
    internal static partial Regex PhoneInputRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\d{2}[A-Z]{5}\d{4}[A-Z]\d[A-Z][A-Z\d]$")]
    private static partial Regex GstinRegex();

    [GeneratedRegex(@"^[A-Z]{5}\d{4}[A-Z]$")]
    private static partial Regex PanRegex();

    private bool HasNoOptionalValidationErrors()
    {
        var stateValid = BusinessProfileRules.IsValidIndianState(State);
        var pincodeValid = string.IsNullOrWhiteSpace(Pincode) || (Pincode.Trim().Length == 6 && Pincode.Trim().AsSpan().IndexOfAnyExceptInRange('0', '9') < 0);
        var emailValid = string.IsNullOrWhiteSpace(Email) || EmailRegex().IsMatch(Email.Trim());
        var phoneNormalized = string.IsNullOrWhiteSpace(Phone) ? string.Empty : Phone.Trim();
        var phoneValid = string.IsNullOrWhiteSpace(phoneNormalized)
            || BusinessProfileRules.IsValidPhone(phoneNormalized);
        var gstinFormatValid = !ShouldValidateAdvancedSetupFields || string.IsNullOrWhiteSpace(GSTIN) || GstinRegex().IsMatch(GSTIN.Trim().ToUpperInvariant());
        var gstinChecksumValid = !ShouldValidateAdvancedSetupFields || string.IsNullOrWhiteSpace(GSTIN) || GSTIN.Trim().Length != 15 || VerifyGstinChecksum(GSTIN.Trim().ToUpperInvariant());
        var panValid = !ShouldValidateAdvancedSetupFields || string.IsNullOrWhiteSpace(PAN) || PanRegex().IsMatch(PAN.Trim().ToUpperInvariant());
        var compositionValid = !ShouldValidateAdvancedSetupFields || !IsCompositionScheme || IsValidCompositionRate(CompositionRate);
        var gstStateConsistent = !ShouldValidateAdvancedSetupFields || IsGstinStateConsistent(GSTIN, State);
        var backupTimeValid = !ShouldValidateAdvancedSetupFields || !AutoBackupEnabled || IsValidBackupTime(BackupTime);
        var backupLocationPresent = !ShouldValidateAdvancedSetupFields || !AutoBackupEnabled || !string.IsNullOrWhiteSpace(BackupLocation);
        var backupPathValid = !ShouldValidateAdvancedSetupFields || !AutoBackupEnabled || string.IsNullOrWhiteSpace(BackupLocation) || IsValidBackupLocationPath(BackupLocation);
        var backupPathExists = !ShouldValidateAdvancedSetupFields || !AutoBackupEnabled || string.IsNullOrWhiteSpace(BackupLocation) || BackupFolderExists(BackupLocation);

        return stateValid
            && pincodeValid
            && emailValid
            && phoneValid
            && gstinFormatValid
            && gstinChecksumValid
            && panValid
            && compositionValid
            && gstStateConsistent
            && backupTimeValid
            && backupLocationPresent
            && backupPathValid
            && backupPathExists;
    }


    private void ClearErrorOnEdit()
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
            ErrorMessage = string.Empty;

        if (!string.IsNullOrEmpty(FirstErrorFieldKey))
            FirstErrorFieldKey = string.Empty;
    }

    private static bool MasterPinContainsRolePin(string master, params string[] rolePins) =>
        rolePins.Any(p => p.Length >= 4 && master.Contains(p, StringComparison.Ordinal));

    private bool HasAnySupportingFirmDetail() =>
        !string.IsNullOrWhiteSpace(Address)
        || !string.IsNullOrWhiteSpace(State)
        || !string.IsNullOrWhiteSpace(Pincode)
        || !string.IsNullOrWhiteSpace(Phone)
        || !string.IsNullOrWhiteSpace(Email);

    private bool HasNoVisibleFirmDetailErrors()
    {
        var stateValid = BusinessProfileRules.IsValidIndianState(State);
        var pincodeValid = string.IsNullOrWhiteSpace(Pincode) || (Pincode.Trim().Length == 6 && Pincode.Trim().AsSpan().IndexOfAnyExceptInRange('0', '9') < 0);
        var emailValid = string.IsNullOrWhiteSpace(Email) || EmailRegex().IsMatch(Email.Trim());
        var phoneNormalized = string.IsNullOrWhiteSpace(Phone) ? string.Empty : Phone.Trim();
        var phoneValid = string.IsNullOrWhiteSpace(phoneNormalized)
            || BusinessProfileRules.IsValidPhone(phoneNormalized);

        return stateValid && pincodeValid && emailValid && phoneValid;
    }

    private void RestoreDraftSafe()
    {
        try
        {
            if (!File.Exists(DraftFilePath))
                return;

            var encryptedBytes = Convert.FromBase64String(File.ReadAllText(DraftFilePath));
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, DraftEntropy, DataProtectionScope.CurrentUser);
            var draft = JsonSerializer.Deserialize<SetupDraftState>(plainBytes);
            if (draft is null)
                return;

            _isRestoringDraft = true;
            FirmName = draft.FirmName ?? string.Empty;
            Address = draft.Address ?? string.Empty;
            State = draft.State ?? string.Empty;
            Pincode = draft.Pincode ?? string.Empty;
            Phone = draft.Phone ?? string.Empty;
            Email = draft.Email ?? string.Empty;
            AdminPin = draft.AdminPin ?? string.Empty;
            AdminPinConfirm = draft.AdminPinConfirm ?? string.Empty;
            UserPin = draft.UserPin ?? string.Empty;
            UserPinConfirm = draft.UserPinConfirm ?? string.Empty;
            MasterPin = draft.MasterPin ?? string.Empty;
            MasterPinConfirm = draft.MasterPinConfirm ?? string.Empty;
            UseEssentialSetupValidationOnly = draft.UseEssentialSetupValidationOnly;
            ShowRolePins = draft.ShowRolePins;
            ShowMasterPins = draft.ShowMasterPins;
            ShowOptionalFirmFields = draft.ShowOptionalFirmFields
                || !string.IsNullOrWhiteSpace(Address)
                || !string.IsNullOrWhiteSpace(State)
                || !string.IsNullOrWhiteSpace(Pincode)
                || !string.IsNullOrWhiteSpace(Phone)
                || !string.IsNullOrWhiteSpace(Email);

            // Keep setup resilient when validation rules evolve:
            // drop previously persisted phone formats that are no longer valid.
            if (!string.IsNullOrWhiteSpace(Phone) && !BusinessProfileRules.IsValidPhone(Phone.Trim()))
                Phone = string.Empty;

            HasRecoveredDraft =
                !string.IsNullOrWhiteSpace(FirmName)
                || !string.IsNullOrWhiteSpace(Address)
                || !string.IsNullOrWhiteSpace(State)
                || !string.IsNullOrWhiteSpace(Pincode)
                || !string.IsNullOrWhiteSpace(Phone)
                || !string.IsNullOrWhiteSpace(Email)
                || !string.IsNullOrWhiteSpace(AdminPin)
                || !string.IsNullOrWhiteSpace(UserPin)
                || !string.IsNullOrWhiteSpace(MasterPin);
        }
        catch
        {
            // Best-effort draft restore; ignore corrupted/incompatible drafts.
        }
        finally
        {
            _isRestoringDraft = false;
        }
    }

    private void AutoSaveDraftSafe()
    {
        if (_isPersistingDraft || _isRestoringDraft || IsSetupComplete || !IsDirty)
            return;

        try
        {
            _isPersistingDraft = true;
            Directory.CreateDirectory(Path.GetDirectoryName(DraftFilePath)!);

            var state = new SetupDraftState
            {
                FirmName = FirmName,
                Address = Address,
                State = State,
                Pincode = Pincode,
                Phone = Phone,
                Email = Email,
                AdminPin = AdminPin,
                AdminPinConfirm = AdminPinConfirm,
                UserPin = UserPin,
                UserPinConfirm = UserPinConfirm,
                MasterPin = MasterPin,
                MasterPinConfirm = MasterPinConfirm,
                UseEssentialSetupValidationOnly = UseEssentialSetupValidationOnly,
                ShowRolePins = ShowRolePins,
                ShowMasterPins = ShowMasterPins,
                ShowOptionalFirmFields = ShowOptionalFirmFields
            };

            var plainBytes = JsonSerializer.SerializeToUtf8Bytes(state);
            var encryptedBytes = ProtectedData.Protect(plainBytes, DraftEntropy, DataProtectionScope.CurrentUser);
            File.WriteAllText(DraftFilePath, Convert.ToBase64String(encryptedBytes));
        }
        catch
        {
            // Best-effort draft autosave.
        }
        finally
        {
            _isPersistingDraft = false;
        }
    }

    private static void DeleteDraftSafe()
    {
        try
        {
            if (File.Exists(DraftFilePath))
                File.Delete(DraftFilePath);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private sealed class SetupDraftState
    {
        public string? FirmName { get; set; }
        public string? Address { get; set; }
        public string? State { get; set; }
        public string? Pincode { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? AdminPin { get; set; }
        public string? AdminPinConfirm { get; set; }
        public string? UserPin { get; set; }
        public string? UserPinConfirm { get; set; }
        public string? MasterPin { get; set; }
        public string? MasterPinConfirm { get; set; }
        public bool UseEssentialSetupValidationOnly { get; set; } = true;
        public bool ShowRolePins { get; set; }
        public bool ShowMasterPins { get; set; }
        public bool ShowOptionalFirmFields { get; set; }
    }

    public override void Dispose()
    {
        PropertyChanged -= OnSetupPropertyChanged;
        CancelRedirectCountdown();
        _draftAutoSaveTimer.Dispose();
        RequestClose = null;
        base.Dispose();
    }
}

