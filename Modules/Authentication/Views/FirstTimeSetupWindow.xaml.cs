using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class FirstTimeSetupWindow : Window
{
    public FirstTimeSetupWindow(FirstTimeSetupViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose = result => DialogResult = result;

        // Enforce numeric-only input on all PIN PasswordBoxes
        AdminPinBox.PreviewTextInput += OnPreviewNumericOnly;
        AdminPinConfirmBox.PreviewTextInput += OnPreviewNumericOnly;
        ManagerPinBox.PreviewTextInput += OnPreviewNumericOnly;
        ManagerPinConfirmBox.PreviewTextInput += OnPreviewNumericOnly;
        UserPinBox.PreviewTextInput += OnPreviewNumericOnly;
        UserPinConfirmBox.PreviewTextInput += OnPreviewNumericOnly;
        MasterPinBox.PreviewTextInput += OnPreviewNumericOnly;
        MasterPinConfirmBox.PreviewTextInput += OnPreviewNumericOnly;

        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is not FirstTimeSetupViewModel vm) return;

        if (vm.IsStep3)
        {
            if (vm.SaveCommand.CanExecute(null))
                vm.SaveCommand.Execute(null);
        }
        else
        {
            vm.NextStepCommand.Execute(null);
        }

        e.Handled = true;
    }

    private void OnPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FirstTimeSetupViewModel vm) return;

        if (sender == AdminPinBox)
            vm.AdminPin = AdminPinBox.Password;
        else if (sender == AdminPinConfirmBox)
            vm.AdminPinConfirm = AdminPinConfirmBox.Password;
        else if (sender == ManagerPinBox)
            vm.ManagerPin = ManagerPinBox.Password;
        else if (sender == ManagerPinConfirmBox)
            vm.ManagerPinConfirm = ManagerPinConfirmBox.Password;
        else if (sender == UserPinBox)
            vm.UserPin = UserPinBox.Password;
        else if (sender == UserPinConfirmBox)
            vm.UserPinConfirm = UserPinConfirmBox.Password;
        else if (sender == MasterPinBox)
            vm.MasterPin = MasterPinBox.Password;
        else if (sender == MasterPinConfirmBox)
            vm.MasterPinConfirm = MasterPinConfirmBox.Password;
    }

    /// <summary>Only allow digit characters in PIN fields.</summary>
    private static void OnPreviewNumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !DigitsOnlyRegex().IsMatch(e.Text);
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
