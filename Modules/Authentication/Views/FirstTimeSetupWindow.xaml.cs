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

        // 16b: Enforce numeric-only input on all PIN PasswordBoxes
        AdminPinBox.PreviewTextInput += OnPreviewNumericOnly;
        ManagerPinBox.PreviewTextInput += OnPreviewNumericOnly;
        UserPinBox.PreviewTextInput += OnPreviewNumericOnly;
        MasterPinBox.PreviewTextInput += OnPreviewNumericOnly;
    }

    private void OnPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FirstTimeSetupViewModel vm) return;

        if (sender == AdminPinBox)
            vm.AdminPin = AdminPinBox.Password;
        else if (sender == ManagerPinBox)
            vm.ManagerPin = ManagerPinBox.Password;
        else if (sender == UserPinBox)
            vm.UserPin = UserPinBox.Password;
        else if (sender == MasterPinBox)
            vm.MasterPin = MasterPinBox.Password;
    }

    /// <summary>16b: Only allow digit characters in PIN fields.</summary>
    private static void OnPreviewNumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !DigitsOnlyRegex().IsMatch(e.Text);
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
