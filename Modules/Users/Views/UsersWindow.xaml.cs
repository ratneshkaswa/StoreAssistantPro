using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Users.ViewModels;

namespace StoreAssistantPro.Modules.Users.Views;

public partial class UsersWindow : BaseDialogWindow
{
    protected override double DialogWidth => 500;
    protected override double DialogHeight => 480;
    protected override double DialogMinWidth => 460;
    protected override double DialogMinHeight => 440;
    protected override bool AllowResize => true;

    public UsersWindow(
        IWindowSizingService sizingService,
        UsersViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        vm.PropertyChanged += OnViewModelPropertyChanged;

        // 7a: Enforce numeric-only input on PIN PasswordBoxes
        NewPinBox.PreviewTextInput += OnPreviewNumericOnly;
        ConfirmPinBox.PreviewTextInput += OnPreviewNumericOnly;
        MasterPinBox.PreviewTextInput += OnPreviewNumericOnly;
        DataObject.AddPastingHandler(NewPinBox, OnPasteNumericOnly);
        DataObject.AddPastingHandler(ConfirmPinBox, OnPasteNumericOnly);
        DataObject.AddPastingHandler(MasterPinBox, OnPasteNumericOnly);

        Closed += (_, _) =>
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.Dispose();
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        RunDeferredInitialLoad(async () =>
        {
            if (DataContext is UsersViewModel vm)
            {
                try
                {
                    await vm.LoadUsersCommand.ExecuteAsync(null);
                }
                catch (Exception)
                {
                    // RunLoadAsync inside the VM already captures and logs
                    // exceptions. This guard is defensive against edge cases
                    // where the command infrastructure itself throws.
                }
            }
        });

    private void OnNewPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsersViewModel vm)
            vm.NewPin = NewPinBox.Password;
    }

    private void OnConfirmPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsersViewModel vm)
            vm.ConfirmPin = ConfirmPinBox.Password;
    }

    private void OnMasterPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsersViewModel vm)
            vm.MasterPin = MasterPinBox.Password;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not UsersViewModel vm) return;

        // Clear password boxes when VM clears the string properties (after successful change)
        if (e.PropertyName == nameof(UsersViewModel.NewPin) && vm.NewPin != NewPinBox.Password)
            NewPinBox.Password = vm.NewPin;
        else if (e.PropertyName == nameof(UsersViewModel.ConfirmPin) && vm.ConfirmPin != ConfirmPinBox.Password)
            ConfirmPinBox.Password = vm.ConfirmPin;
        else if (e.PropertyName == nameof(UsersViewModel.MasterPin) && vm.MasterPin != MasterPinBox.Password)
            MasterPinBox.Password = vm.MasterPin;
    }

    /// <summary>7a: Only allow digit characters in PIN fields.</summary>
    private static void OnPreviewNumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !DigitsOnlyRegex().IsMatch(e.Text);
    }

    private static void OnPasteNumericOnly(object sender, DataObjectPastingEventArgs e)
    {
        var pastedText =
            e.DataObject.GetData(DataFormats.UnicodeText) as string ??
            e.DataObject.GetData(DataFormats.Text) as string;

        if (string.IsNullOrEmpty(pastedText) || !DigitsOnlyRegex().IsMatch(pastedText))
            e.CancelCommand();
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
