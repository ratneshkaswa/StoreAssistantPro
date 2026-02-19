using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.SystemSettings.ViewModels;

namespace StoreAssistantPro.Modules.SystemSettings.Views;

public partial class SecuritySettingsView : UserControl
{
    public SecuritySettingsView()
    {
        InitializeComponent();
    }

    private void CurrentPinBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SecuritySettingsViewModel vm)
            vm.CurrentMasterPin = CurrentPinBox.Password;
    }

    private void NewPinBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SecuritySettingsViewModel vm)
            vm.NewMasterPin = NewPinBox.Password;
    }

    private void ConfirmPinBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SecuritySettingsViewModel vm)
            vm.ConfirmMasterPin = ConfirmPinBox.Password;
    }
}
