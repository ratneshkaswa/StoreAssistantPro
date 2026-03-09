using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Modules.Authentication.Commands;

namespace StoreAssistantPro.Modules.Authentication.ViewModels;

public partial class SetupViewModel : BaseViewModel
{
    // ── Firm details ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string FirmName { get; set; } = string.Empty;

    partial void OnFirmNameChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    public partial string Address { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string State { get; set; } = string.Empty;

    public ObservableCollection<string> IndianStates { get; } =
    [
        "Andhra Pradesh", "Arunachal Pradesh", "Assam", "Bihar", "Chhattisgarh",
        "Goa", "Gujarat", "Haryana", "Himachal Pradesh", "Jharkhand",
        "Karnataka", "Kerala", "Madhya Pradesh", "Maharashtra", "Manipur",
        "Meghalaya", "Mizoram", "Nagaland", "Odisha", "Punjab",
        "Rajasthan", "Sikkim", "Tamil Nadu", "Telangana", "Tripura",
        "Uttar Pradesh", "Uttarakhand", "West Bengal",
        "Andaman & Nicobar", "Chandigarh", "Dadra & Nagar Haveli",
        "Delhi", "Jammu & Kashmir", "Ladakh", "Lakshadweep", "Puducherry"
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PincodeValidationHint))]
    public partial string Pincode { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhoneValidationHint))]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GstinValidationHint))]
    [NotifyPropertyChangedFor(nameof(GstinPanCrossHint))]
    public partial string GSTIN { get; set; } = string.Empty;

    partial void OnGSTINChanged(string value)
    {
        // Auto-fill State from valid GSTIN state code
        if (value.Length >= 2 && GstinRegex().IsMatch(value.ToUpperInvariant()))
        {
            var stateName = GetGstinStateName(value[..2]);
            if (stateName != null && string.IsNullOrWhiteSpace(State))
                State = stateName;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PanValidationHint))]
    [NotifyPropertyChangedFor(nameof(GstinPanCrossHint))]
    public partial string PAN { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmailValidationHint))]
    public partial string Email { get; set; } = string.Empty;

    /// <summary>Currency code is always INR — India-exclusive app.</summary>
    public string CurrencyCode => "INR";

    // ── Regional settings ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrencyPreview))]
    public partial string SelectedCurrencySymbol { get; set; } = "₹";

    public ObservableCollection<string> CurrencySymbols { get; } = ["₹", "Rs."];

    public ObservableCollection<string> Months { get; } = new(
        Enumerable.Range(1, 12).Select(m => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m)));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinancialYearDisplay))]
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
            try { return $"e.g. {DateTime.Today.ToString(SelectedDateFormat, CultureInfo.InvariantCulture)}"; }
            catch { return string.Empty; }
        }
    }

    public string CurrencyPreview => $"e.g. {SelectedCurrencySymbol} 1,00,000";

    // ── Live field validation hints ──

    public string PhoneValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Phone)) return string.Empty;
            if (!PhoneInputRegex().IsMatch(Phone)) return "Digits, +, - and spaces only";
            var digits = new string(Phone.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
                return $"✓ {digits[..5]} {digits[5..]}";
            if (digits.Length == 12 && Phone.TrimStart().StartsWith('+'))
                return $"✓ +{digits[..2]} {digits[2..7]} {digits[7..]}";
            return "✓";
        }
    }

    public string EmailValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Email)) return string.Empty;
            return EmailRegex().IsMatch(Email) ? "✓" : "Enter a valid email address";
        }
    }

    public string GstinValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(GSTIN)) return string.Empty;
            if (!GstinRegex().IsMatch(GSTIN)) return "Format: 22AAAAA0000A1Z5";
            var stateCode = GSTIN[..2];
            var stateName = GetGstinStateName(stateCode);
            var prefix = stateName != null ? $"✓ State: {stateName}" : "✓";
            if (GSTIN.Length == 15 && !VerifyGstinChecksum(GSTIN))
                return $"{prefix} — check digit mismatch";
            return prefix;
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

    private static string? GetGstinStateName(string code) => code switch
    {
        "01" => "Jammu & Kashmir", "02" => "Himachal Pradesh", "03" => "Punjab",
        "04" => "Chandigarh", "05" => "Uttarakhand", "06" => "Haryana",
        "07" => "Delhi", "08" => "Rajasthan", "09" => "Uttar Pradesh",
        "10" => "Bihar", "11" => "Sikkim", "12" => "Arunachal Pradesh",
        "13" => "Nagaland", "14" => "Manipur", "15" => "Mizoram",
        "16" => "Tripura", "17" => "Meghalaya", "18" => "Assam",
        "19" => "West Bengal", "20" => "Jharkhand", "21" => "Odisha",
        "22" => "Chhattisgarh", "23" => "Madhya Pradesh", "24" => "Gujarat",
        "26" => "Dadra & Nagar Haveli", "27" => "Maharashtra", "29" => "Karnataka",
        "30" => "Goa", "31" => "Lakshadweep", "32" => "Kerala",
        "33" => "Tamil Nadu", "34" => "Puducherry", "35" => "Andaman & Nicobar",
        "36" => "Telangana", "37" => "Andhra Pradesh", "38" => "Ladakh",
        _ => null
    };

    public string PanValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PAN)) return string.Empty;
            if (!PanRegex().IsMatch(PAN)) return "Format: ABCDE1234F";
            var entityType = PAN.Length >= 4 ? GetPanEntityType(PAN[3]) : null;
            return entityType != null ? $"✓ {entityType}" : "✓";
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
            return Pincode.Length == 6 && Pincode.AsSpan().IndexOfAnyExceptInRange('0', '9') < 0
                ? "✓" : "Must be exactly 6 digits";
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
                ? "✓ PAN matches GSTIN"
                : "⚠ PAN does not match GSTIN characters 3–12";
        }
    }

    // ── PIN setup ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdminPinWarning))]
    [NotifyPropertyChangedFor(nameof(AdminPinWarningDetail))]
    [NotifyPropertyChangedFor(nameof(AdminPinStrength))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string AdminPin { get; set; } = string.Empty;

    partial void OnAdminPinChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdminConfirmHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string AdminPinConfirm { get; set; } = string.Empty;

    partial void OnAdminPinConfirmChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManagerPinWarning))]
    [NotifyPropertyChangedFor(nameof(ManagerPinWarningDetail))]
    [NotifyPropertyChangedFor(nameof(ManagerPinStrength))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string ManagerPin { get; set; } = string.Empty;

    partial void OnManagerPinChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManagerConfirmHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string ManagerPinConfirm { get; set; } = string.Empty;

    partial void OnManagerPinConfirmChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserPinWarning))]
    [NotifyPropertyChangedFor(nameof(UserPinWarningDetail))]
    [NotifyPropertyChangedFor(nameof(UserPinStrength))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string UserPin { get; set; } = string.Empty;

    partial void OnUserPinChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserConfirmHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string UserPinConfirm { get; set; } = string.Empty;

    partial void OnUserPinConfirmChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MasterConfirmHint))]
    [NotifyPropertyChangedFor(nameof(MasterPinWarning))]
    [NotifyPropertyChangedFor(nameof(MasterPinWarningDetail))]
    [NotifyPropertyChangedFor(nameof(MasterPinStrength))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string MasterPin { get; set; } = string.Empty;

    partial void OnMasterPinChanged(string value) => ClearErrorOnEdit();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MasterConfirmHint))]
    [NotifyPropertyChangedFor(nameof(RequiredFieldsProgress))]
    public partial string MasterPinConfirm { get; set; } = string.Empty;

    partial void OnMasterPinConfirmChanged(string value) => ClearErrorOnEdit();

    // ── Weak PIN warnings (non-blocking guidance) ──

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

    // ── Required fields progress ──

    public string RequiredFieldsProgress
    {
        get
        {
            var done = 0;
            const int total = 5;

            if (!string.IsNullOrWhiteSpace(FirmName)) done++;
            if (InputValidator.IsValidUserPin(AdminPin) && AdminPin == AdminPinConfirm) done++;
            if (InputValidator.IsValidUserPin(ManagerPin) && ManagerPin == ManagerPinConfirm) done++;
            if (InputValidator.IsValidUserPin(UserPin) && UserPin == UserPinConfirm) done++;
            if (InputValidator.IsValidMasterPin(MasterPin) && MasterPin == MasterPinConfirm) done++;

            return done == total ? "✓ Ready" : $"{done} of {total} required groups complete";
        }
    }

    // S10: Setup complete state
    [ObservableProperty]
    public partial bool IsSetupComplete { get; set; }

    [ObservableProperty]
    public partial string RedirectCountdown { get; set; } = string.Empty;

    public Action<bool?>? RequestClose { get; set; }

    public SetupViewModel(ICommandBus commandBus) : base()
    {
        _commandBus = commandBus;
    }

    private readonly ICommandBus _commandBus;

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(FirmName), "Firm name is required.")
            .Rule(InputValidator.IsValidUserPin(AdminPin), "Admin PIN must be exactly 4 digits.")
            .Rule(InputValidator.AreEqual(AdminPin, AdminPinConfirm), "Admin PIN confirmation does not match.")
            .Rule(InputValidator.IsValidUserPin(ManagerPin), "Manager PIN must be exactly 4 digits.")
            .Rule(InputValidator.AreEqual(ManagerPin, ManagerPinConfirm), "Manager PIN confirmation does not match.")
            .Rule(InputValidator.IsValidUserPin(UserPin), "User PIN must be exactly 4 digits.")
            .Rule(InputValidator.AreEqual(UserPin, UserPinConfirm), "User PIN confirmation does not match.")
            .Rule(InputValidator.AreAllDistinct(AdminPin, ManagerPin, UserPin), "Each role must have a unique PIN.")
            .Rule(InputValidator.IsValidMasterPin(MasterPin), "Master PIN must be exactly 6 digits.")
            .Rule(InputValidator.AreEqual(MasterPin, MasterPinConfirm), "Master PIN confirmation does not match.")
            .Rule(!MasterPinContainsRolePin(MasterPin, AdminPin, ManagerPin, UserPin), "Master PIN must not contain any role PIN.")
            .Rule(string.IsNullOrWhiteSpace(GSTIN) || GstinRegex().IsMatch(GSTIN.Trim().ToUpperInvariant()), "GSTIN format is invalid.")
            .Rule(string.IsNullOrWhiteSpace(GSTIN) || GSTIN.Trim().Length != 15 || VerifyGstinChecksum(GSTIN.Trim().ToUpperInvariant()), "GSTIN check digit is invalid.")
            .Rule(string.IsNullOrWhiteSpace(PAN) || PanRegex().IsMatch(PAN.Trim().ToUpperInvariant()), "PAN format is invalid.")
            .Rule(string.IsNullOrWhiteSpace(Pincode) || (Pincode.Trim().Length == 6 && Pincode.Trim().AsSpan().IndexOfAnyExceptInRange('0', '9') < 0), "Pincode must be exactly 6 digits.")
            .Rule(string.IsNullOrWhiteSpace(Email) || EmailRegex().IsMatch(Email.Trim()), "Email format is invalid.")
            .Rule(string.IsNullOrWhiteSpace(Phone) || PhoneInputRegex().IsMatch(Phone.Trim()), "Phone may only contain digits, +, - and spaces.")))
            return;

        var fyStartMonth = MonthNameToIndex(SelectedFYStartMonth);
        var fyEndMonth = (fyStartMonth + 10) % 12 + 1;

        var result = await _commandBus.SendAsync(new CompleteFirstSetupCommand(
            FirmName.Trim(), Address.Trim(), State.Trim(), Pincode.Trim(),
            Phone.Trim(), Email.Trim(), GSTIN.Trim().ToUpperInvariant(),
            PAN.Trim().ToUpperInvariant(), CurrencyCode,
            SelectedCurrencySymbol, fyStartMonth, fyEndMonth, SelectedDateFormat,
            AdminPin, ManagerPin, UserPin, MasterPin), ct);

        if (result.Succeeded)
        {
            IsSetupComplete = true;
            for (var i = 3; i >= 1; i--)
            {
                RedirectCountdown = $"Redirecting to login in {i}…";
                await Task.Delay(500, ct);
            }

            ClearSensitivePins();
            RequestClose?.Invoke(true);
        }
        else
            ErrorMessage = result.ErrorMessage ?? "Setup failed.";
    });

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

    private const string WeakPinShortText = "⚠ Weak PIN";
    private const string WeakPinDetailText = "⚠ Weak PIN — consider a less predictable combination.";
    private const string WeakMasterPinShortText = "⚠ Weak PIN";
    private const string WeakMasterPinDetailText = "⚠ Weak — consider a less predictable combination.";

    private static bool IsWeakUserPin(string pin) =>
        pin is "0000" or "1234" or "4321" or "1111" or "2222" or "3333"
            or "4444" or "5555" or "6666" or "7777" or "8888" or "9999";

    private static bool IsWeakMasterPin(string pin) =>
        pin is "000000" or "123456" or "654321" or "111111" or "222222" or "333333"
            or "444444" or "555555" or "666666" or "777777" or "888888" or "999999";

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
        return confirm == pin ? "✓ Match" : "✗ Does not match";
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
            ? $"⚠ PIN conflicts: {string.Join(", ", conflicts)}"
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

    [GeneratedRegex(@"^[\d\s\+\-]+$")]
    internal static partial Regex PhoneInputRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\d{2}[A-Z]{5}\d{4}[A-Z]\d[Z][A-Z\d]$")]
    private static partial Regex GstinRegex();

    [GeneratedRegex(@"^[A-Z]{5}\d{4}[A-Z]$")]
    private static partial Regex PanRegex();

    private static int MonthNameToIndex(string name)
    {
        for (var i = 1; i <= 12; i++)
            if (CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(i) == name)
                return i;
        return 4;
    }

    private void ClearErrorOnEdit()
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
            ErrorMessage = string.Empty;
    }

    private static bool MasterPinContainsRolePin(string master, params string[] rolePins) =>
        rolePins.Any(p => p.Length >= 4 && master.Contains(p, StringComparison.Ordinal));
}
