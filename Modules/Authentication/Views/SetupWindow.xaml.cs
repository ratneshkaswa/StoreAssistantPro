using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class SetupWindow : Window
{
    private SetupViewModel? _vm;

    public SetupWindow(SetupViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        vm.RequestClose = result => DialogResult = result;

        SourceInitialized += (_, _) => Win11Backdrop.Apply(this, useMicaAlt: true);

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

        // Focus firm name on load
        Loaded += (_, _) => FirmNameBox.Focus();
        Closed += (_, _) => vm.Dispose();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is not SetupViewModel vm) return;

        // IsBusy guard — don't fire commands while a save is in progress
        if (vm.IsBusy) { e.Handled = true; return; }

        if (vm.SaveCommand.CanExecute(null))
            vm.SaveCommand.Execute(null);

        e.Handled = true;
    }

    private void OnPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SetupViewModel vm) return;

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

    /// <summary>Confirm close if setup is in progress.</summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_vm is null || _vm.IsSetupComplete || DialogResult == true) return;

        var result = MessageBox.Show(
            "Setup is not complete. Are you sure you want to cancel?",
            "Cancel Setup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
            e.Cancel = true;
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}