using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Modules.SystemSettings.Commands;

namespace StoreAssistantPro.Modules.SystemSettings.ViewModels;

public partial class SecuritySettingsViewModel(ICommandBus commandBus) : BaseViewModel
{
    [ObservableProperty]
    public partial string CurrentMasterPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewMasterPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmMasterPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SuccessMessage { get; set; } = string.Empty;

    [RelayCommand]
    private async Task ChangeMasterPinAsync()
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(CurrentMasterPin), "Current Master Password is required.")
            .Rule(InputValidator.IsValidMasterPin(NewMasterPin), "New Master Password must be exactly 6 digits.")
            .Rule(InputValidator.AreEqual(NewMasterPin, ConfirmMasterPin), "New passwords do not match.")))
            return;

        var result = await commandBus.SendAsync(
            new ChangeMasterPinCommand(CurrentMasterPin, NewMasterPin));

        if (result.Succeeded)
        {
            SuccessMessage = "Master Password changed successfully.";
            CurrentMasterPin = string.Empty;
            NewMasterPin = string.Empty;
            ConfirmMasterPin = string.Empty;
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Change failed.";
            CurrentMasterPin = string.Empty;
        }
    }

    }
