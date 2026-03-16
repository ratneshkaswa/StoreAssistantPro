using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using StoreAssistantPro.Modules.Firm.ViewModels;

namespace StoreAssistantPro.Modules.Firm.Views;

public partial class FirmManagementView : UserControl
{
    private static readonly Dictionary<string, string> FieldFocusMap = new(StringComparer.Ordinal)
    {
        [nameof(FirmViewModel.FirmName)] = "FirmNameBox",
        [nameof(FirmViewModel.Address)] = "AddressBox",
        [nameof(FirmViewModel.State)] = "StateCombo",
        [nameof(FirmViewModel.Pincode)] = "PincodeBox",
        [nameof(FirmViewModel.Phone)] = "PhoneBox",
        [nameof(FirmViewModel.Email)] = "EmailBox",
        [nameof(FirmViewModel.SelectedGstRegistrationType)] = "GstTypeCombo",
        [nameof(FirmViewModel.CompositionRate)] = "CompositionRateBox",
        [nameof(FirmViewModel.GSTNumber)] = "GstinBox",
        [nameof(FirmViewModel.PANNumber)] = "PanBox",
        [nameof(FirmViewModel.SelectedCurrencySymbol)] = "CurrencyCombo",
        [nameof(FirmViewModel.SelectedFYStartMonth)] = "FyStartMonthCombo",
        [nameof(FirmViewModel.SelectedDateFormat)] = "DateFormatCombo",
        [nameof(FirmViewModel.SelectedTaxMode)] = "TaxModeCombo",
        [nameof(FirmViewModel.SelectedRoundingMethod)] = "RoundingCombo",
        [nameof(FirmViewModel.SelectedNumberToWordsLanguage)] = "NumberToWordsCombo"
    };

    public FirmManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FirmViewModel vm)
            return;

        vm.PropertyChanged += OnViewModelPropertyChanged;
        Unloaded += (_, _) => vm.PropertyChanged -= OnViewModelPropertyChanged;

        try
        {
            await vm.LoadFirmCommand.ExecuteAsync(null);

            _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (!string.IsNullOrWhiteSpace(vm.FirstErrorFieldKey))
                {
                    FocusFieldByKey(vm.FirstErrorFieldKey, vm);
                    return;
                }

                TryFocusControl("FirmNameBox");
            });
        }
        catch
        {
            // RunLoadAsync inside the VM already captures and logs exceptions.
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not FirmViewModel vm)
            return;

        if (e.PropertyName == nameof(FirmViewModel.FirstErrorFieldKey)
            && !string.IsNullOrWhiteSpace(vm.FirstErrorFieldKey))
        {
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => FocusFieldByKey(vm.FirstErrorFieldKey, vm));
        }
    }

    private void FocusFieldByKey(string fieldKey, FirmViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(fieldKey))
            return;

        if (!FieldFocusMap.TryGetValue(fieldKey, out var controlName))
            return;

        if (fieldKey == nameof(FirmViewModel.CompositionRate) && !vm.IsCompositionScheme)
            controlName = "GstTypeCombo";

        TryFocusControl(controlName);
    }

    private void TryFocusControl(string controlName)
    {
        if (FindName(controlName) is not FrameworkElement element)
            return;

        if (!element.IsVisible || !element.IsEnabled)
            return;

        element.BringIntoView();
        element.Focus();

        if (element is TextBox textBox)
            textBox.SelectAll();
    }
}
