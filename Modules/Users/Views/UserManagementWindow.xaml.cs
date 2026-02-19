using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Users.ViewModels;

namespace StoreAssistantPro.Modules.Users.Views;

public partial class UserManagementWindow : Window
{
    public UserManagementWindow(UserManagementViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel vm)
            await vm.LoadUsersCommand.ExecuteAsync(null);
    }

    private void OnNewPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel vm)
            vm.NewPin = NewPinBox.Password;
    }

    private void OnConfirmPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel vm)
            vm.ConfirmPin = ConfirmPinBox.Password;
    }

    private void OnMasterPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel vm)
            vm.MasterPin = MasterPinBox.Password;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not UserManagementViewModel vm) return;

        // Clear password boxes when VM clears the string properties (after successful change)
        if (e.PropertyName == nameof(UserManagementViewModel.NewPin) && vm.NewPin != NewPinBox.Password)
            NewPinBox.Password = vm.NewPin;
        else if (e.PropertyName == nameof(UserManagementViewModel.ConfirmPin) && vm.ConfirmPin != ConfirmPinBox.Password)
            ConfirmPinBox.Password = vm.ConfirmPin;
        else if (e.PropertyName == nameof(UserManagementViewModel.MasterPin) && vm.MasterPin != MasterPinBox.Password)
            MasterPinBox.Password = vm.MasterPin;
    }
}
