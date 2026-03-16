using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Modules.Users.ViewModels;

namespace StoreAssistantPro.Modules.Users.Views;

public partial class UserManagementView : UserControl
{
    private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);
    public UserManagementView()
    {
        InitializeComponent();

        NewPinBox.PreviewTextInput += OnPreviewNumericOnly;
        ConfirmPinBox.PreviewTextInput += OnPreviewNumericOnly;
        MasterPinBox.PreviewTextInput += OnPreviewNumericOnly;
        DataObject.AddPastingHandler(NewPinBox, OnPasteNumericOnly);
        DataObject.AddPastingHandler(ConfirmPinBox, OnPasteNumericOnly);
        DataObject.AddPastingHandler(MasterPinBox, OnPasteNumericOnly);

        Loaded += (_, _) =>
        {
            if (DataContext is UsersViewModel vm)
                vm.PropertyChanged += OnViewModelPropertyChanged;
        };

        Unloaded += (_, _) =>
        {
            if (DataContext is UsersViewModel vm)
                vm.PropertyChanged -= OnViewModelPropertyChanged;
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsersViewModel vm)
        {
            try { await vm.LoadUsersCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }

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

        if (e.PropertyName == nameof(UsersViewModel.NewPin) && vm.NewPin != NewPinBox.Password)
            NewPinBox.Password = vm.NewPin;
        else if (e.PropertyName == nameof(UsersViewModel.ConfirmPin) && vm.ConfirmPin != ConfirmPinBox.Password)
            ConfirmPinBox.Password = vm.ConfirmPin;
        else if (e.PropertyName == nameof(UsersViewModel.MasterPin) && vm.MasterPin != MasterPinBox.Password)
            MasterPinBox.Password = vm.MasterPin;
    }

    private static void OnPreviewNumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !DigitsOnly.IsMatch(e.Text);
    }

    private static void OnPasteNumericOnly(object sender, DataObjectPastingEventArgs e)
    {
        var pastedText =
            e.DataObject.GetData(DataFormats.UnicodeText) as string ??
            e.DataObject.GetData(DataFormats.Text) as string;

        if (string.IsNullOrEmpty(pastedText) || !DigitsOnly.IsMatch(pastedText))
            e.CancelCommand();
    }
}
