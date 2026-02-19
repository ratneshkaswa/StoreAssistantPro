using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Users.Commands;
using StoreAssistantPro.Modules.Users.Services;

namespace StoreAssistantPro.Modules.Users.ViewModels;

public partial class UserManagementViewModel(
    IUserService userService,
    ICommandBus commandBus) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<UserCredential> Users { get; set; } = [];

    [ObservableProperty]
    public partial UserCredential? SelectedUser { get; set; }

    [ObservableProperty]
    public partial string NewPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MasterPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsMasterPinRequired { get; set; }

    [ObservableProperty]
    public partial string SuccessMessage { get; set; } = string.Empty;

    partial void OnSelectedUserChanged(UserCredential? value)
    {
        NewPin = string.Empty;
        ConfirmPin = string.Empty;
        MasterPin = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsMasterPinRequired = value?.UserType == UserType.Admin;
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            var users = await userService.GetAllUsersAsync();
            Users = new ObservableCollection<UserCredential>(users);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ChangePinAsync()
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v
            .Rule(SelectedUser is not null, "Please select a user.")
            .Rule(InputValidator.IsValidUserPin(NewPin), "New PIN must be exactly 4 digits.")
            .Rule(InputValidator.AreEqual(NewPin, ConfirmPin), "PINs do not match.")))
            return;

        var result = await commandBus.SendAsync(new ChangePinCommand(
            SelectedUser.UserType, NewPin,
            SelectedUser.UserType == UserType.Admin ? MasterPin : null));

        if (result.Succeeded)
        {
            SuccessMessage = $"{SelectedUser.UserType} PIN changed successfully.";
            NewPin = string.Empty;
            ConfirmPin = string.Empty;
            MasterPin = string.Empty;
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "PIN change failed.";
            if (SelectedUser.UserType == UserType.Admin)
                MasterPin = string.Empty;
        }
    }

    }
