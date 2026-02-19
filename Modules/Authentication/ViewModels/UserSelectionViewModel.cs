using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Authentication.ViewModels;

public partial class UserSelectionViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial UserType SelectedUserType { get; set; }

    public Action<bool?>? RequestClose { get; set; }

    [RelayCommand]
    private void SelectAdmin()
    {
        SelectedUserType = UserType.Admin;
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void SelectManager()
    {
        SelectedUserType = UserType.Manager;
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void SelectUser()
    {
        SelectedUserType = UserType.User;
        RequestClose?.Invoke(true);
    }
}
