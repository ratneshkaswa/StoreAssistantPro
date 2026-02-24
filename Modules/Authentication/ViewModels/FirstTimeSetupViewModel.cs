using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Modules.Authentication.Commands;

namespace StoreAssistantPro.Modules.Authentication.ViewModels;

public partial class FirstTimeSetupViewModel : BaseViewModel
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
    public partial string Address { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    // ── Step 2: PIN setup ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdminPinWarning))]
    public partial string AdminPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AdminPinConfirm { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManagerPinWarning))]
    public partial string ManagerPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ManagerPinConfirm { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserPinWarning))]
    public partial string UserPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserPinConfirm { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MasterPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MasterPinConfirm { get; set; } = string.Empty;

    // ── Weak PIN warnings (non-blocking guidance) ──

    public string AdminPinWarning => GetPinWarning(AdminPin);
    public string ManagerPinWarning => GetPinWarning(ManagerPin);
    public string UserPinWarning => GetPinWarning(UserPin);

    public Action<bool?>? RequestClose { get; set; }

    public FirstTimeSetupViewModel(ICommandBus commandBus) : base()
    {
        _commandBus = commandBus;
        CurrentStep = 1;
    }

    private readonly ICommandBus _commandBus;

    [RelayCommand]
    private void NextStep()
    {
        ErrorMessage = string.Empty;

        if (CurrentStep == 1)
        {
            if (!Validate(v => v
                .Rule(InputValidator.IsRequired(FirmName), "Firm name is required.")))
                return;
            CurrentStep = 2;
        }
        else if (CurrentStep == 2)
        {
            if (!Validate(v => v
                .Rule(InputValidator.IsValidUserPin(AdminPin), "Admin PIN must be exactly 4 digits.")
                .Rule(InputValidator.AreEqual(AdminPin, AdminPinConfirm), "Admin PIN confirmation does not match.")
                .Rule(InputValidator.IsValidUserPin(ManagerPin), "Manager PIN must be exactly 4 digits.")
                .Rule(InputValidator.AreEqual(ManagerPin, ManagerPinConfirm), "Manager PIN confirmation does not match.")
                .Rule(InputValidator.IsValidUserPin(UserPin), "User PIN must be exactly 4 digits.")
                .Rule(InputValidator.AreEqual(UserPin, UserPinConfirm), "User PIN confirmation does not match.")
                .Rule(InputValidator.AreAllDistinct(AdminPin, ManagerPin, UserPin), "Each role must have a unique PIN.")
                .Rule(InputValidator.IsValidMasterPin(MasterPin), "Master Password must be exactly 6 digits.")
                .Rule(InputValidator.AreEqual(MasterPin, MasterPinConfirm), "Master Password confirmation does not match.")))
                return;
            CurrentStep = 3;
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 1)
        {
            ErrorMessage = string.Empty;
            CurrentStep--;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var result = await _commandBus.SendAsync(new CompleteFirstSetupCommand(
            FirmName.Trim(), Address.Trim(), Phone.Trim(),
            AdminPin, ManagerPin, UserPin, MasterPin));

        if (result.Succeeded)
            RequestClose?.Invoke(true);
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
}
