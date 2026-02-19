using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Modules.Authentication.Commands;

namespace StoreAssistantPro.Modules.Authentication.ViewModels;

public partial class FirstTimeSetupViewModel(ICommandBus commandBus) : BaseViewModel
{
    [ObservableProperty]
    public partial string FirmName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AdminPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ManagerPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MasterPin { get; set; } = string.Empty;

    public Action<bool?>? RequestClose { get; set; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(FirmName), "Firm name is required.")
            .Rule(InputValidator.IsValidUserPin(AdminPin), "Admin PIN must be exactly 4 digits.")
            .Rule(InputValidator.IsValidUserPin(ManagerPin), "Manager PIN must be exactly 4 digits.")
            .Rule(InputValidator.IsValidUserPin(UserPin), "User PIN must be exactly 4 digits.")
            .Rule(InputValidator.AreAllDistinct(AdminPin, ManagerPin, UserPin), "Each role must have a unique PIN.")
            .Rule(InputValidator.IsValidMasterPin(MasterPin), "Master Password must be exactly 6 digits.")))
            return;

        var result = await commandBus.SendAsync(new CompleteFirstSetupCommand(
            FirmName.Trim(), AdminPin, ManagerPin, UserPin, MasterPin));

        if (result.Succeeded)
            RequestClose?.Invoke(true);
        else
            ErrorMessage = result.ErrorMessage ?? "Setup failed.";
    }

    }
