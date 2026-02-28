using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Modules.SystemSettings.ViewModels;

namespace StoreAssistantPro.Modules.SystemSettings.Views;

public partial class SecuritySettingsView : UserControl
{
    public SecuritySettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // 11a: Enforce numeric-only input on master password PasswordBoxes
        CurrentPinBox.PreviewTextInput += OnPreviewNumericOnly;
        NewPinBox.PreviewTextInput += OnPreviewNumericOnly;
        ConfirmPinBox.PreviewTextInput += OnPreviewNumericOnly;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not SecuritySettingsViewModel vm) return;

        // 11c: Sync PasswordBox when ViewModel clears fields after success
        if (e.PropertyName is nameof(SecuritySettingsViewModel.CurrentMasterPin)
            && vm.CurrentMasterPin.Length == 0 && CurrentPinBox.Password.Length > 0)
            CurrentPinBox.Clear();

        if (e.PropertyName is nameof(SecuritySettingsViewModel.NewMasterPin)
            && vm.NewMasterPin.Length == 0 && NewPinBox.Password.Length > 0)
            NewPinBox.Clear();

        if (e.PropertyName is nameof(SecuritySettingsViewModel.ConfirmMasterPin)
            && vm.ConfirmMasterPin.Length == 0 && ConfirmPinBox.Password.Length > 0)
            ConfirmPinBox.Clear();
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

    /// <summary>11a: Only allow digit characters in password fields.</summary>
    private static void OnPreviewNumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !DigitsOnlyRegex().IsMatch(e.Text);
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
