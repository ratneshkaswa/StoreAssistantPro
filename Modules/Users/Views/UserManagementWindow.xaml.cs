using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Users.ViewModels;

namespace StoreAssistantPro.Modules.Users.Views;

public partial class UserManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 500;
    protected override double DialogHeight => 480;

    public UserManagementWindow(
        IWindowSizingService sizingService,
        UserManagementViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        vm.PropertyChanged += OnViewModelPropertyChanged;

        // 7a: Enforce numeric-only input on PIN PasswordBoxes
        NewPinBox.PreviewTextInput += OnPreviewNumericOnly;
        ConfirmPinBox.PreviewTextInput += OnPreviewNumericOnly;
        MasterPinBox.PreviewTextInput += OnPreviewNumericOnly;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel vm)
            await vm.LoadUsersCommand.ExecuteAsync(null);
    }

    private void OnNewPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel vm)
            vm.NewPin = NewPinBox.Password;
    }

    private void OnConfirmPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel vm)
            vm.ConfirmPin = ConfirmPinBox.Password;
    }

    private void OnMasterPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel vm)
            vm.MasterPin = MasterPinBox.Password;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not UserManagementViewModel vm) return;

        // Clear password boxes when VM clears the string properties (after successful change)
        if (e.PropertyName == nameof(UserManagementViewModel.NewPin) && vm.NewPin != NewPinBox.Password)
            NewPinBox.Password = vm.NewPin;
        else if (e.PropertyName == nameof(UserManagementViewModel.ConfirmPin) && vm.ConfirmPin != ConfirmPinBox.Password)
            ConfirmPinBox.Password = vm.ConfirmPin;
        else if (e.PropertyName == nameof(UserManagementViewModel.MasterPin) && vm.MasterPin != MasterPinBox.Password)
            MasterPinBox.Password = vm.MasterPin;
    }

    /// <summary>7a: Only allow digit characters in PIN fields.</summary>
    private static void OnPreviewNumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !DigitsOnlyRegex().IsMatch(e.Text);
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
