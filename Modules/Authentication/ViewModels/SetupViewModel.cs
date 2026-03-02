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
    // ── Step navigation ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(StepDisplay))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    public partial int CurrentStep { get; set; }

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool CanGoBack => CurrentStep > 1;
    public string StepDisplay => $"Step {CurrentStep} of 3";

    // ── Step 1: Firm details ──

    [ObservableProperty]
    public partial string FirmName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressDisplay))]
    public partial string Address { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhoneValidationHint))]
    [NotifyPropertyChangedFor(nameof(PhoneDisplay))]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GstinValidationHint))]
    [NotifyPropertyChangedFor(nameof(GstinDisplay))]
    public partial string GSTIN { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmailValidationHint))]
    [NotifyPropertyChangedFor(nameof(EmailDisplay))]
    public partial string Email { get; set; } = string.Empty;

    /// <summary>Currency is always INR — India-exclusive app.</summary>
    public string CurrencyCode => "INR";

    // Step 3 summary display — shows "Not provided" for blank optional fields
    public string AddressDisplay => string.IsNullOrWhiteSpace(Address) ? "Not provided" : Address;
    public string PhoneDisplay => string.IsNullOrWhiteSpace(Phone) ? "Not provided" : Phone;
    public string EmailDisplay => string.IsNullOrWhiteSpace(Email) ? "Not provided" : Email;
    public string GstinDisplay => string.IsNullOrWhiteSpace(GSTIN) ? "Not provided" : GSTIN;

    // ── Live field validation hints ──

    public string PhoneValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Phone)) return string.Empty;
            if (!PhoneRegex().IsMatch(Phone)) return "Digits, +, - and spaces only";
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
            return stateName != null ? $"✓ State: {stateName}" : "✓";
        }
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

    // ── Step 2: PIN setup ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdminPinWarning))]
    [NotifyPropertyChangedFor(nameof(AdminPinStrength))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    public partial string AdminPin { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdminConfirmHint))]
    public partial string AdminPinConfirm { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManagerPinWarning))]
    [NotifyPropertyChangedFor(nameof(ManagerPinStrength))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    public partial string ManagerPin { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManagerConfirmHint))]
    public partial string ManagerPinConfirm { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserPinWarning))]
    [NotifyPropertyChangedFor(nameof(UserPinStrength))]
    [NotifyPropertyChangedFor(nameof(PinConflictWarning))]
    public partial string UserPin { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserConfirmHint))]
    public partial string UserPinConfirm { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MasterConfirmHint))]
    public partial string MasterPin { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MasterConfirmHint))]
    public partial string MasterPinConfirm { get; set; } = string.Empty;

    // ── Weak PIN warnings (non-blocking guidance) ──

    public string AdminPinWarning => GetPinWarning(AdminPin);
    public string ManagerPinWarning => GetPinWarning(ManagerPin);
    public string UserPinWarning => GetPinWarning(UserPin);

    // S5: PIN strength (0-3: empty, weak, fair, strong)
    public int AdminPinStrength => GetPinStrength(AdminPin);
    public int ManagerPinStrength => GetPinStrength(ManagerPin);
    public int UserPinStrength => GetPinStrength(UserPin);

    // Live confirm mismatch hints
    public string AdminConfirmHint => GetConfirmHint(AdminPin, AdminPinConfirm, 4);
    public string ManagerConfirmHint => GetConfirmHint(ManagerPin, ManagerPinConfirm, 4);
    public string UserConfirmHint => GetConfirmHint(UserPin, UserPinConfirm, 4);
    public string MasterConfirmHint => GetConfirmHint(MasterPin, MasterPinConfirm, 6);

    // S7: Live cross-validation
    public string PinConflictWarning => GetPinConflictWarning();

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
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

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
            .Rule(InputValidator.AreEqual(MasterPin, MasterPinConfirm), "Master PIN confirmation does not match.")))
            return;

        var result = await _commandBus.SendAsync(new CompleteFirstSetupCommand(
            FirmName.Trim(), Address.Trim(), Phone.Trim(),
            Email.Trim(), GSTIN.Trim().ToUpperInvariant(), CurrencyCode,
            AdminPin, ManagerPin, UserPin, MasterPin));

        if (result.Succeeded)
        {
            IsSetupComplete = true;
            for (var i = 3; i >= 1; i--)
            {
                RedirectCountdown = $"Redirecting to login in {i}…";
                await Task.Delay(500);
            }
            RequestClose?.Invoke(true);
        }
        else
            ErrorMessage = result.ErrorMessage ?? "Setup failed.";
    }

    private static string GetPinWarning(string pin)
    {
        if (pin.Length < 4) return string.Empty;
        if (pin is "0000" or "1234" or "4321" or "1111" or "2222" or "3333"
            or "4444" or "5555" or "6666" or "7777" or "8888" or "9999")
            return "⚠ Weak PIN — consider a less predictable combination.";
        return string.Empty;
    }

    // S5: PIN strength meter (0=empty, 1=weak, 2=fair, 3=strong)
    private static int GetPinStrength(string pin)
    {
        if (pin.Length < 4) return 0;
        if (pin is "0000" or "1234" or "4321" or "1111" or "2222" or "3333"
            or "4444" or "5555" or "6666" or "7777" or "8888" or "9999")
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
        if (AdminPin.Length < 4 && ManagerPin.Length < 4 && UserPin.Length < 4)
            return string.Empty;

        var conflicts = new List<string>();
        if (AdminPin.Length == 4 && ManagerPin.Length == 4 && AdminPin == ManagerPin)
            conflicts.Add("Admin = Manager");
        if (AdminPin.Length == 4 && UserPin.Length == 4 && AdminPin == UserPin)
            conflicts.Add("Admin = User");
        if (ManagerPin.Length == 4 && UserPin.Length == 4 && ManagerPin == UserPin)
            conflicts.Add("Manager = User");

        return conflicts.Count > 0
            ? $"⚠ Duplicate PINs: {string.Join(", ", conflicts)}"
            : string.Empty;
    }

    [GeneratedRegex(@"^[\d\s\+\-]+$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\d{2}[A-Z]{5}\d{4}[A-Z]\d[Z][A-Z\d]$")]
    private static partial Regex GstinRegex();
}
