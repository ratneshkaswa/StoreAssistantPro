using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class FirstTimeSetupWindow : Window
{
    private FirstTimeSetupViewModel? _vm;

    public FirstTimeSetupWindow(FirstTimeSetupViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
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

        // Auto-focus first field when step changes
        vm.StepChanged += OnStepChanged;

        // Focus firm name on load
        Loaded += (_, _) => FocusByName("FirmNameBox");
    }

    private void OnStepChanged(int step)
    {
        Dispatcher.BeginInvoke(() =>
        {
            switch (step)
            {
                case 1:
                    FocusByName("FirmNameBox");
                    break;
                case 2:
                    AdminPinBox.Focus();
                    break;
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void FocusByName(string name)
    {
        if (FindName(name) is UIElement element)
            element.Focus();
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

    /// <summary>Only allow digits, +, -, and spaces in phone field.</summary>
    private void OnPreviewPhoneOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !PhoneCharsRegex().IsMatch(e.Text);
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

    [GeneratedRegex(@"^[\d\s\+\-]+$")]
    private static partial Regex PhoneCharsRegex();
}
