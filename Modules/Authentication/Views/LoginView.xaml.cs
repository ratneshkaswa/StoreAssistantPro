using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class LoginView : UserControl
{
    private LoginViewModel? _vm;

    private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);

    public LoginView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Subscribe to keyboard events at the window level
        var window = Window.GetWindow(this);
        if (window is not null)
        {
            window.PreviewKeyDown += OnPreviewKeyDown;
            window.PreviewKeyUp += (_, _) => UpdateCapsLockWarning();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is not null)
        {
            window.PreviewKeyDown -= OnPreviewKeyDown;
        }

        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
            _vm.ResetCompleted -= OnResetCompleted;
            _vm = null;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
            _vm.ResetCompleted -= OnResetCompleted;
        }

        if (e.NewValue is LoginViewModel newVm)
        {
            _vm = newVm;
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            newVm.ResetCompleted += OnResetCompleted;

            // Numeric-only + paste protection on reset PIN fields
            PasswordBox[] resetPinBoxes = [ResetNewPinBox, ResetConfirmPinBox, ResetMasterBox];
            foreach (var box in resetPinBoxes)
            {
                box.PreviewTextInput += OnPreviewNumericOnly;
                DataObject.AddPastingHandler(box, OnPastingNumericOnly);
            }
        }
        else
        {
            _vm = null;
        }
    }

    /// <summary>
    /// Routes physical keyboard input to PIN pad commands and role
    /// selection shortcuts so the entire login flow is keyboard-driven.
    /// F1/F2 select Admin/User respectively.
    /// ESC uses layered escape: clear PIN first, then deselect role.
    /// In forgot-PIN mode, ESC cancels and Enter resets.
    /// </summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_vm is not LoginViewModel vm)
            return;

        // Only handle keys when this login view is visible
        if (!IsVisible)
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
            case Key.F2 when vm.SelectUserCommand.CanExecute(UserType.User):
                vm.SelectUserCommand.Execute(UserType.User);
                e.Handled = true;
                return;
        }

        if (!vm.IsPinRequired)
            return;

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
        if (_vm is not LoginViewModel vm) return;

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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not LoginViewModel vm)
            return;

        if (e.PropertyName == nameof(LoginViewModel.SelectedUserType) && vm.IsPinRequired && !vm.IsForgotPinMode)
        {
            Dispatcher.BeginInvoke(() => Digit1Button.Focus(),
                System.Windows.Threading.DispatcherPriority.Input);
        }

        if (e.PropertyName == nameof(LoginViewModel.IsForgotPinMode) && vm.IsForgotPinMode)
        {
            Dispatcher.BeginInvoke(() => ResetMasterBox.Focus(),
                System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void OnResetCompleted() => ClearResetFields();

    private static void OnPreviewNumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !DigitsOnly.IsMatch(e.Text);
    }

    private static void OnPastingNumericOnly(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            if (!DigitsOnly.IsMatch(text))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    private void UpdateCapsLockWarning()
    {
        CapsLockWarning.Visibility = Console.CapsLock
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
