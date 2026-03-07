using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class SetupWindow : Window
{
    private SetupViewModel? _vm;

    public SetupWindow(IWindowSizingService sizing, SetupViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        vm.RequestClose = result => DialogResult = result;

        sizing.ConfigureStartupWindow(this, 640, 820);
        SourceInitialized += (_, _) => Win11Backdrop.Apply(this, useMicaAlt: true);

        // Enforce numeric-only input + paste protection on all PIN PasswordBoxes
        PasswordBox[] pinBoxes = [AdminPinBox, AdminPinConfirmBox, ManagerPinBox, ManagerPinConfirmBox,
                                   UserPinBox, UserPinConfirmBox, MasterPinBox, MasterPinConfirmBox];
        foreach (var box in pinBoxes)
        {
            box.PreviewTextInput += OnPreviewNumericOnly;
            DataObject.AddPastingHandler(box, OnPastingNumericOnly);
        }

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

        // Don't trigger save when focus is inside editable fields
        if (Keyboard.FocusedElement is TextBox or PasswordBox or ComboBox)
            return;

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

    /// <summary>Reject non-numeric clipboard paste in PIN fields.</summary>
    private static void OnPastingNumericOnly(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            if (!DigitsOnlyRegex().IsMatch(text))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    /// <summary>Confirm close if setup is in progress.</summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_vm is null || _vm.IsSetupComplete || DialogResult == true) return;

        // Prevent close during processing
        if (_vm.IsBusy) { e.Cancel = true; return; }

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