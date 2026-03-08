using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class LoginWindow : Window
{
    private LoginViewModel? _vm;

    public LoginWindow(IWindowSizingService sizing, LoginViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        vm.RequestClose = result => DialogResult = result;
        vm.Initialize();

        sizing.ConfigureStartupWindow(this, 480, 740);
        SourceInitialized += (_, _) => Win11Backdrop.Apply(this);

        PreviewKeyDown += OnPreviewKeyDown;
        PreviewKeyUp += (_, _) => UpdateCapsLockWarning();

        // Numeric-only + paste protection on reset PIN fields
        PasswordBox[] resetPinBoxes = [ResetNewPinBox, ResetConfirmPinBox, ResetMasterBox];
        foreach (var box in resetPinBoxes)
        {
            box.PreviewTextInput += OnPreviewNumericOnly;
            DataObject.AddPastingHandler(box, OnPastingNumericOnly);
        }

        // Auto-focus first keypad button after role selection
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.SelectedUserType) && vm.IsUserSelected && !vm.IsForgotPinMode)
                Dispatcher.BeginInvoke(() => Digit1Button.Focus(),
                    System.Windows.Threading.DispatcherPriority.Input);

            // Auto-focus master PIN box when entering forgot PIN mode
            if (e.PropertyName == nameof(LoginViewModel.IsForgotPinMode) && vm.IsForgotPinMode)
                Dispatcher.BeginInvoke(() => ResetMasterBox.Focus(),
                    System.Windows.Threading.DispatcherPriority.Input);
        };

        // Clear PasswordBoxes after successful PIN reset
        vm.ResetCompleted += ClearResetFields;

        Closed += (_, _) => vm.Dispose();
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

        UpdateCapsLockWarning();

        // In forgot-PIN mode, Enter triggers reset; ESC cancels
        if (vm.IsForgotPinMode)
        {
            if (e.Key == Key.Escape && vm.CancelForgotPinCommand.CanExecute(null))
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

        // Keyboard safety — skip PIN pad routing when focus is inside editable fields
        var focusInEditable = Keyboard.FocusedElement is TextBox or PasswordBox or ComboBox;

        // Layered ESC: clear PIN first, then deselect role
        if (e.Key == Key.Escape)
        {
            if (vm.PinPad.PinLength > 0 && vm.PinPad.ClearCommand.CanExecute(null))
                vm.PinPad.ClearCommand.Execute(null);
            else if (vm.IsUserSelected)
                vm.DeselectRole();
            e.Handled = true;
            return;
        }

        // Role selection shortcuts (always active regardless of focus)
        switch (e.Key)
        {
            case Key.F1 when vm.SelectUserCommand.CanExecute(UserType.Admin):
                vm.SelectUserCommand.Execute(UserType.Admin);
                e.Handled = true;
                return;
            case Key.F2 when vm.SelectUserCommand.CanExecute(UserType.Manager):
                vm.SelectUserCommand.Execute(UserType.Manager);
                e.Handled = true;
                return;
            case Key.F3 when vm.SelectUserCommand.CanExecute(UserType.User):
                vm.SelectUserCommand.Execute(UserType.User);
                e.Handled = true;
                return;
        }

        // Skip digit/key routing when focus is inside editable fields
        if (focusInEditable)
            return;

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

        if (digit is not null && vm.PinPad.AddDigitCommand.CanExecute(digit))
        {
            vm.PinPad.AddDigitCommand.Execute(digit);
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Back when vm.PinPad.BackspaceCommand.CanExecute(null):
                vm.PinPad.BackspaceCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Delete when vm.PinPad.ClearCommand.CanExecute(null):
                vm.PinPad.ClearCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Enter when vm.LoginCommand.CanExecute(null):
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

    /// <summary>Prevent window closing during processing.</summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // Allow programmatic close after successful login
        if (DialogResult == true) return;

        if (_vm is { IsBusy: true } or { IsVerifying: true })
            e.Cancel = true;
    }

    private void UpdateCapsLockWarning()
    {
        CapsLockWarning.Visibility = Keyboard.IsKeyToggled(Key.CapsLock)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
