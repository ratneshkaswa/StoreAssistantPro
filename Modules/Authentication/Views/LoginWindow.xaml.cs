using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose = result => DialogResult = result;
        vm.Initialize();

        PreviewKeyDown += OnPreviewKeyDown;

        // Numeric-only on reset PIN fields
        ResetNewPinBox.PreviewTextInput += OnPreviewNumericOnly;
        ResetConfirmPinBox.PreviewTextInput += OnPreviewNumericOnly;
        ResetMasterBox.PreviewTextInput += OnPreviewNumericOnly;

        // Auto-focus first keypad button after role selection
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.SelectedUserType) && vm.IsUserSelected && !vm.IsForgotPinMode)
                Dispatcher.BeginInvoke(() => Digit1Button.Focus(),
                    System.Windows.Threading.DispatcherPriority.Input);

            // Auto-focus master password box when entering forgot PIN mode
            if (e.PropertyName == nameof(LoginViewModel.IsForgotPinMode) && vm.IsForgotPinMode)
                Dispatcher.BeginInvoke(() => ResetMasterBox.Focus(),
                    System.Windows.Threading.DispatcherPriority.Input);
        };
    }

    /// <summary>
    /// Routes physical keyboard input to PIN pad commands and role
    /// selection shortcuts so the entire login flow is keyboard-driven.
    /// F1/F2/F3 select Admin/Manager/User respectively.
    /// ESC uses layered escape: clear PIN first, then deselect role.
    /// In forgot-PIN mode, ESC cancels and Enter resets.
    /// </summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not LoginViewModel vm)
            return;

        // In forgot-PIN mode, Enter triggers reset; ESC cancels
        if (vm.IsForgotPinMode)
        {
            if (e.Key == Key.Escape)
            {
                vm.CancelForgotPinCommand.Execute(null);
                ClearResetFields();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && vm.ResetPinCommand.CanExecute(null))
            {
                vm.ResetPinCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        // Layered ESC: clear PIN first, then deselect role
        if (e.Key == Key.Escape)
        {
            if (vm.PinPad.PinLength > 0)
                vm.PinPad.ClearCommand.Execute(null);
            else if (vm.IsUserSelected)
                vm.DeselectRole();
            e.Handled = true;
            return;
        }

        // Role selection shortcuts
        switch (e.Key)
        {
            case Key.F1:
                vm.SelectUserCommand.Execute(UserType.Admin);
                e.Handled = true;
                return;
            case Key.F2:
                vm.SelectUserCommand.Execute(UserType.Manager);
                e.Handled = true;
                return;
            case Key.F3:
                vm.SelectUserCommand.Execute(UserType.User);
                e.Handled = true;
                return;
        }

        var digit = e.Key switch
        {
            Key.D0 or Key.NumPad0 => "0",
            Key.D1 or Key.NumPad1 => "1",
            Key.D2 or Key.NumPad2 => "2",
            Key.D3 or Key.NumPad3 => "3",
            Key.D4 or Key.NumPad4 => "4",
            Key.D5 or Key.NumPad5 => "5",
            Key.D6 or Key.NumPad6 => "6",
            Key.D7 or Key.NumPad7 => "7",
            Key.D8 or Key.NumPad8 => "8",
            Key.D9 or Key.NumPad9 => "9",
            _ => null
        };

        if (digit is not null)
        {
            vm.PinPad.AddDigitCommand.Execute(digit);
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Back:
                vm.PinPad.BackspaceCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Delete:
                vm.PinPad.ClearCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Enter:
                if (vm.LoginCommand.CanExecute(null))
                    vm.LoginCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void OnResetPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm) return;

        if (sender == ResetMasterBox)
            vm.MasterPassword = ResetMasterBox.Password;
        else if (sender == ResetNewPinBox)
            vm.NewPin = ResetNewPinBox.Password;
        else if (sender == ResetConfirmPinBox)
            vm.NewPinConfirm = ResetConfirmPinBox.Password;
    }

    private void ClearResetFields()
    {
        ResetMasterBox.Password = string.Empty;
        ResetNewPinBox.Password = string.Empty;
        ResetConfirmPinBox.Password = string.Empty;
    }

    private static void OnPreviewNumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !DigitsOnlyRegex().IsMatch(e.Text);
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
