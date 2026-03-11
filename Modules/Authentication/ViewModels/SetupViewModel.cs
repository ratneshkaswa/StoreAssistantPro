using System.Collections.ObjectModel;
using System.Globalization;
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
    private static readonly (string Code, string Name)[] IndianStateData =
    [
        ("37", "Andhra Pradesh"),
        ("12", "Arunachal Pradesh"),
        ("18", "Assam"),
        ("10", "Bihar"),
        ("22", "Chhattisgarh"),
        ("30", "Goa"),
        ("24", "Gujarat"),
        ("06", "Haryana"),
        ("02", "Himachal Pradesh"),
        ("20", "Jharkhand"),
        ("29", "Karnataka"),
        ("32", "Kerala"),
        ("23", "Madhya Pradesh"),
        ("27", "Maharashtra"),
        ("14", "Manipur"),
        ("17", "Meghalaya"),
        ("15", "Mizoram"),
        ("13", "Nagaland"),
        ("21", "Odisha"),
        ("03", "Punjab"),
        ("08", "Rajasthan"),
        ("11", "Sikkim"),
        ("33", "Tamil Nadu"),
        ("36", "Telangana"),
        ("16", "Tripura"),
        ("09", "Uttar Pradesh"),
        ("05", "Uttarakhand"),
        ("19", "West Bengal"),
        ("35", "Andaman & Nicobar"),
        ("04", "Chandigarh"),
        ("26", "Dadra & Nagar Haveli"),
        ("07", "Delhi"),
        ("01", "Jammu & Kashmir"),
        ("38", "Ladakh"),
        ("31", "Lakshadweep"),
        ("34", "Puducherry")
    ];

    private static readonly Dictionary<string, string> IndianStateNameByCode =
        IndianStateData.ToDictionary(state => state.Code, state => state.Name);

    private static readonly Dictionary<string, string> IndianStateCodeByName =
        IndianStateData.ToDictionary(state => state.Name, state => state.Code, StringComparer.OrdinalIgnoreCase);

    // -- Firm details --

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsFirmSectionComplete))]
    public partial string FirmName { get; set; } = string.Empty;

    partial void OnFirmNameChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string Address { get; set; } = string.Empty;

    partial void OnAddressChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DerivedStateCode))]
    [NotifyPropertyChangedFor(nameof(DerivedStateCodeDisplay))]
    [NotifyPropertyChangedFor(nameof(GstinValidationHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string State { get; set; } = string.Empty;

    partial void OnStateChanged(string value) => ClearErrorOnEdit();

    public ObservableCollection<string> IndianStates { get; } =
        new(IndianStateData.Select(state => state.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PincodeValidationHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string Pincode { get; set; } = string.Empty;

    partial void OnPincodeChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhoneValidationHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string Phone { get; set; } = string.Empty;

    partial void OnPhoneChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GstinValidationHint))]
    [NotifyPropertyChangedFor(nameof(GstinPanCrossHint))]
    [NotifyPropertyChangedFor(nameof(DerivedStateCode))]
    [NotifyPropertyChangedFor(nameof(DerivedStateCodeDisplay))]
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
            if (!PhoneInputRegex().IsMatch(Phone)) return "Digits, +, - and spaces only";
            var digits = new string(Phone.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
                return $"\u2713 {digits[..5]} {digits[5..]}";
            if (digits.Length == 12 && Phone.TrimStart().StartsWith('+'))
                return $"\u2713 +{digits[..2]} {digits[2..7]} {digits[7..]}";
            if (digits.Length < 10)
                return "At least 10 digits expected";
            return "\u2713";
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
    public static bool VerifyGstinChecksum(string gstin)
    {
        if (gstin.Length != 15) return false;
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var sum = 0;
        for (var i = 0; i < 14; i++)
        {
            var idx = chars.IndexOf(char.ToUpperInvariant(gstin[i]));
            if (idx < 0) return false;
            var factor = (i % 2 == 0) ? 1 : 2;
            var product = idx * factor;
            sum += product / 36 + product % 36;
        }
        var checkIdx = (36 - sum % 36) % 36;
        return char.ToUpperInvariant(gstin[14]) == chars[checkIdx];
    }

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
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    public partial string AdminPin { get; set; } = string.Empty;

    partial void OnAdminPinChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdminConfirmHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    public partial string AdminPinConfirm { get; set; } = string.Empty;

    partial void OnAdminPinConfirmChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManagerPinWarning))]
    [NotifyPropertyChangedFor(nameof(ManagerPinWarningDetail))]
    [NotifyPropertyChangedFor(nameof(ManagerPinStrength))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    public partial string ManagerPin { get; set; } = string.Empty;

    partial void OnManagerPinChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManagerConfirmHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    public partial string ManagerPinConfirm { get; set; } = string.Empty;

    partial void OnManagerPinConfirmChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserPinWarning))]
    [NotifyPropertyChangedFor(nameof(UserPinWarningDetail))]
    [NotifyPropertyChangedFor(nameof(UserPinStrength))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    public partial string UserPin { get; set; } = string.Empty;

    partial void OnUserPinChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserConfirmHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    public partial string UserPinConfirm { get; set; } = string.Empty;

    partial void OnUserPinConfirmChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MasterConfirmHint))]
    [NotifyPropertyChangedFor(nameof(MasterPinWarning))]
    [NotifyPropertyChangedFor(nameof(MasterPinWarningDetail))]
    [NotifyPropertyChangedFor(nameof(MasterPinStrength))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    public partial string MasterPin { get; set; } = string.Empty;

    partial void OnMasterPinChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MasterConfirmHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    [NotifyPropertyChangedFor(nameof(IsSecuritySectionComplete))]
    public partial string MasterPinConfirm { get; set; } = string.Empty;

    partial void OnMasterPinConfirmChanged(string value) => ClearErrorOnEdit();

    // -- Weak PIN warnings (non-blocking guidance) --

    public string AdminPinWarning => GetPinWarning(AdminPin);
    public string AdminPinWarningDetail => GetPinWarningDetail(AdminPin);
    public string ManagerPinWarning => GetPinWarning(ManagerPin);
    public string ManagerPinWarningDetail => GetPinWarningDetail(ManagerPin);
    public string UserPinWarning => GetPinWarning(UserPin);
    public string UserPinWarningDetail => GetPinWarningDetail(UserPin);
    public string MasterPinWarning => GetMasterPinWarning(MasterPin);
    public string MasterPinWarningDetail => GetMasterPinWarningDetail(MasterPin);

    // S5: PIN strength (0-3: empty, weak, fair, strong)
    public int AdminPinStrength => GetPinStrength(AdminPin);
    public int ManagerPinStrength => GetPinStrength(ManagerPin);
    public int UserPinStrength => GetPinStrength(UserPin);
    public int MasterPinStrength => GetMasterPinStrength(MasterPin);

    // Live confirm mismatch hints
    public string AdminConfirmHint => GetConfirmHint(AdminPin, AdminPinConfirm, 4);
    public string ManagerConfirmHint => GetConfirmHint(ManagerPin, ManagerPinConfirm, 4);
    public string UserConfirmHint => GetConfirmHint(UserPin, UserPinConfirm, 4);
    public string MasterConfirmHint => GetConfirmHint(MasterPin, MasterPinConfirm, 6);

    // S7: Live cross-validation
    public string PinConflictWarning => GetPinConflictWarning();

    // -- Section completion indicators --

    public bool IsFirmSectionComplete => !string.IsNullOrWhiteSpace(FirmName);

    public bool IsSecuritySectionComplete =>
        InputValidator.IsValidUserPin(AdminPin) && AdminPin == AdminPinConfirm &&
        InputValidator.IsValidUserPin(ManagerPin) && ManagerPin == ManagerPinConfirm &&
        InputValidator.IsValidUserPin(UserPin) && UserPin == UserPinConfirm &&
        InputValidator.IsValidMasterPin(MasterPin) && MasterPin == MasterPinConfirm &&
        InputValidator.AreAllDistinct(AdminPin, ManagerPin, UserPin) &&
        !MasterPinContainsRolePin(MasterPin, AdminPin, ManagerPin, UserPin);

    // -- Required fields progress --

    public string RequiredFieldsProgress
    {
        get
        {
            var done = 0;
            const int total = 6;

            if (!string.IsNullOrWhiteSpace(FirmName)) done++;
            if (InputValidator.IsValidUserPin(AdminPin) && AdminPin == AdminPinConfirm) done++;
            if (InputValidator.IsValidUserPin(ManagerPin) && ManagerPin == ManagerPinConfirm) done++;
            if (InputValidator.IsValidUserPin(UserPin) && UserPin == UserPinConfirm) done++;
            if (InputValidator.IsValidMasterPin(MasterPin) && MasterPin == MasterPinConfirm) done++;
            if (InputValidator.AreAllDistinct(AdminPin, ManagerPin, UserPin)
                && !MasterPinContainsRolePin(MasterPin, AdminPin, ManagerPin, UserPin)) done++;

            if (done < total)
                return $"{done} of {total} required checks complete";

            return HasNoOptionalValidationErrors()
                ? "\u2713 Ready"
                : "6 of 6 required checks complete - review optional field errors";
        }
    }

    // -- Sidebar navigation --

    [ObservableProperty]
    public partial string SelectedSection { get; set; } = "Firm";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial bool UseEssentialSetupValidationOnly { get; set; }

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
    }

    private readonly ICommandBus _commandBus;
    private readonly IRegionalSettingsService _regionalSettings;
    private CancellationTokenSource? _redirectCts;

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(FirmName), "Firm name is required.", "FirmName")
            .Rule(InputValidator.IsValidUserPin(AdminPin), "Admin PIN must be exactly 4 digits.", "AdminPin")
            .Rule(InputValidator.AreEqual(AdminPin, AdminPinConfirm), "Admin PIN confirmation does not match.", "AdminPinConfirm")
            .Rule(InputValidator.IsValidUserPin(ManagerPin), "Manager PIN must be exactly 4 digits.", "ManagerPin")
            .Rule(InputValidator.AreEqual(ManagerPin, ManagerPinConfirm), "Manager PIN confirmation does not match.", "ManagerPinConfirm")
            .Rule(InputValidator.IsValidUserPin(UserPin), "User PIN must be exactly 4 digits.", "UserPin")
            .Rule(InputValidator.AreEqual(UserPin, UserPinConfirm), "User PIN confirmation does not match.", "UserPinConfirm")
            .Rule(InputValidator.AreAllDistinct(AdminPin, ManagerPin, UserPin), "Each role must have a unique PIN.", "PinConflict")
            .Rule(InputValidator.IsValidMasterPin(MasterPin), "Master PIN must be exactly 6 digits.", "MasterPin")
            .Rule(InputValidator.AreEqual(MasterPin, MasterPinConfirm), "Master PIN confirmation does not match.", "MasterPinConfirm")
            .Rule(!MasterPinContainsRolePin(MasterPin, AdminPin, ManagerPin, UserPin), "Master PIN must not contain any role PIN.", "MasterPinContains")
            .Rule(!ShouldValidateAdvancedSetupFields || !IsCompositionScheme || IsValidCompositionRate(CompositionRate), "Composition rate must be a valid number (0-100).", "CompositionRate")
            .Rule(!ShouldValidateAdvancedSetupFields || string.IsNullOrWhiteSpace(GSTIN) || GstinRegex().IsMatch(GSTIN.Trim().ToUpperInvariant()), "GSTIN format is invalid.", "GSTIN")
            .Rule(!ShouldValidateAdvancedSetupFields || string.IsNullOrWhiteSpace(GSTIN) || GSTIN.Trim().Length != 15 || VerifyGstinChecksum(GSTIN.Trim().ToUpperInvariant()), "GSTIN check digit is invalid.", "GSTINChecksum")
            .Rule(!ShouldValidateAdvancedSetupFields || string.IsNullOrWhiteSpace(PAN) || PanRegex().IsMatch(PAN.Trim().ToUpperInvariant()), "PAN format is invalid.", "PAN")
            .Rule(!ShouldValidateAdvancedSetupFields || IsGstinStateConsistent(GSTIN, State), "GSTIN state code does not match selected state.", "State")
            .Rule(string.IsNullOrWhiteSpace(Pincode) || (Pincode.Trim().Length == 6 && Pincode.Trim().AsSpan().IndexOfAnyExceptInRange('0', '9') < 0), "Pincode must be exactly 6 digits.", "Pincode")
            .Rule(string.IsNullOrWhiteSpace(Email) || EmailRegex().IsMatch(Email.Trim()), "Email format is invalid.", "Email")
            .Rule(string.IsNullOrWhiteSpace(State) || IndianStateCodeByName.ContainsKey(State.Trim()), "Please select a valid Indian state from the list.", "State")
            .Rule(string.IsNullOrWhiteSpace(Phone) || PhoneInputRegex().IsMatch(Phone.Trim()), "Phone may only contain digits, +, - and spaces.", "Phone")
            .Rule(string.IsNullOrWhiteSpace(Phone) || new string(Phone.Where(char.IsDigit).ToArray()).Length >= 10, "Phone must have at least 10 digits.", "Phone")
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
            AdminPin, ManagerPin, UserPin, MasterPin,
            new SetupBusinessOptions(
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
            StartRedirectCountdown();
        }
        else
            ErrorMessage = result.ErrorMessage ?? "Setup failed.";
    });

    [RelayCommand]
    private void CancelSetup()
    {
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
            for (var i = 3; i >= 1; i--)
            {
                RedirectCountdown = $"Redirecting to login in {i}...";
                await Task.Delay(1000, ct);
            }

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
        ManagerPin = string.Empty;
        ManagerPinConfirm = string.Empty;
        UserPin = string.Empty;
        UserPinConfirm = string.Empty;
        MasterPin = string.Empty;
        MasterPinConfirm = string.Empty;
    }

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

        if (AdminPin.Length == 4 && ManagerPin.Length == 4 && AdminPin == ManagerPin)
            conflicts.Add("Admin = Manager");
        if (AdminPin.Length == 4 && UserPin.Length == 4 && AdminPin == UserPin)
            conflicts.Add("Admin = User");
        if (ManagerPin.Length == 4 && UserPin.Length == 4 && ManagerPin == UserPin)
            conflicts.Add("Manager = User");

        // Master PIN must also be unique from role PINs
        if (MasterPin.Length == 6)
        {
            if (AdminPin.Length == 4 && MasterPin.Contains(AdminPin))
                conflicts.Add("Master contains Admin");
            if (ManagerPin.Length == 4 && MasterPin.Contains(ManagerPin))
                conflicts.Add("Master contains Manager");
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

    [GeneratedRegex(@"^[\d\s\+\-]*\d[\d\s\+\-]*$")]
    internal static partial Regex PhoneInputRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\d{2}[A-Z]{5}\d{4}[A-Z]\d[A-Z][A-Z\d]$")]
    private static partial Regex GstinRegex();

    [GeneratedRegex(@"^[A-Z]{5}\d{4}[A-Z]$")]
    private static partial Regex PanRegex();

    private bool HasNoOptionalValidationErrors()
    {
        var stateValid = string.IsNullOrWhiteSpace(State) || IndianStateCodeByName.ContainsKey(State.Trim());
        var pincodeValid = string.IsNullOrWhiteSpace(Pincode) || (Pincode.Trim().Length == 6 && Pincode.Trim().AsSpan().IndexOfAnyExceptInRange('0', '9') < 0);
        var emailValid = string.IsNullOrWhiteSpace(Email) || EmailRegex().IsMatch(Email.Trim());
        var phoneNormalized = string.IsNullOrWhiteSpace(Phone) ? string.Empty : Phone.Trim();
        var phoneValid = string.IsNullOrWhiteSpace(phoneNormalized)
            || (PhoneInputRegex().IsMatch(phoneNormalized) && new string(phoneNormalized.Where(char.IsDigit).ToArray()).Length >= 10);
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

    public override void Dispose()
    {
        CancelRedirectCountdown();
        RequestClose = null;
        base.Dispose();
    }
}

