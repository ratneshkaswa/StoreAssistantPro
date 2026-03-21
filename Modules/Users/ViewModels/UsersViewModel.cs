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

public partial class UsersViewModel(
    IUserService userService,
    ICommandBus commandBus) : BaseViewModel
{
    [NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
    [ObservableProperty]
    public partial ObservableCollection<UserCredential> Users { get; set; } = [];

    [NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
    [ObservableProperty]
    public partial string NewPin { get; set; } = string.Empty;

    [NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
    [ObservableProperty]
    public partial string ConfirmPin { get; set; } = string.Empty;

    [NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
    [ObservableProperty]
    public partial string MasterPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsMasterPinRequired { get; set; }

    [NotifyCanExecuteChangedFor(nameof(ChangePinCommand))]
    [ObservableProperty]
    public partial UserCredential? SelectedUser { get; set; }

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
    private Task LoadUsersAsync() => RunLoadAsync(async ct =>
    {
        var users = await userService.GetAllUsersAsync(ct);
        Users = new ObservableCollection<UserCredential>(users);
    });

    [RelayCommand(CanExecute = nameof(CanChangePin))]
    private Task ChangePinAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;
        var selectedUser = SelectedUser;

        if (!Validate(v => v
            .Rule(selectedUser is not null, "Please select a user.")
            .Rule(InputValidator.IsValidUserPin(NewPin), "New PIN must be exactly 4 digits.")
            .Rule(!InputValidator.IsWeakPin(NewPin), "PIN is too weak (e.g. 0000, 1234). Choose a stronger PIN.")
            .Rule(InputValidator.AreEqual(NewPin, ConfirmPin), "PINs do not match.")
            .Rule(selectedUser?.UserType != UserType.Admin || InputValidator.IsValidMasterPin(MasterPin),
                "Master PIN must be exactly 6 digits.")))
            return;

        var user = selectedUser!;
        var result = await commandBus.SendAsync(new ChangePinCommand(
            user.UserType, NewPin,
            user.UserType == UserType.Admin ? MasterPin : null));

        if (result.Succeeded)
        {
            SuccessMessage = $"{user.UserType} PIN changed successfully.";
            NewPin = string.Empty;
            ConfirmPin = string.Empty;
            MasterPin = string.Empty;
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "PIN change failed.";
            if (user.UserType == UserType.Admin)
                MasterPin = string.Empty;
        }
    });

    private bool CanChangePin()
    {
        if (SelectedUser is null)
            return false;

        if (!InputValidator.IsValidUserPin(NewPin) || !InputValidator.IsValidUserPin(ConfirmPin))
            return false;

        return SelectedUser.UserType != UserType.Admin || InputValidator.IsValidMasterPin(MasterPin);
    }
}
