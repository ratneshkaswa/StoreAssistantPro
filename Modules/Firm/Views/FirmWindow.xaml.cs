using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Firm.ViewModels;

namespace StoreAssistantPro.Modules.Firm.Views;

public partial class FirmWindow : BaseDialogWindow
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

    private readonly IDialogService _dialogService;
    private readonly FirmViewModel _vm;

    protected override double DialogWidth => 980;
    protected override double DialogHeight => 860;
    protected override double DialogMinWidth => 820;
    protected override double DialogMinHeight => 760;
    protected override bool AllowResize => true;

    public FirmWindow(
        IWindowSizingService sizingService,
        IDialogService dialogService,
        FirmViewModel vm) : base(sizingService)
    {
        _dialogService = dialogService;
        _vm = vm;

        InitializeComponent();
        DataContext = vm;

        vm.PropertyChanged += OnViewModelPropertyChanged;
        Closing += OnWindowClosing;
        Closed += (_, _) =>
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.Dispose();
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _vm.LoadFirmCommand.ExecuteAsync(null);

            _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (!string.IsNullOrWhiteSpace(_vm.FirstErrorFieldKey))
                {
                    FocusFieldByKey(_vm.FirstErrorFieldKey);
                    return;
                }

                TryFocusControl("FirmNameBox");
            });
        }
        catch (Exception)
        {
            // RunLoadAsync inside the VM already captures and logs
            // exceptions. This guard is defensive against command errors.
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not FirmViewModel vm)
            return;

        if (e.PropertyName == nameof(FirmViewModel.FirstErrorFieldKey)
            && !string.IsNullOrWhiteSpace(vm.FirstErrorFieldKey))
        {
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => FocusFieldByKey(vm.FirstErrorFieldKey));
        }
    }

    private void FocusFieldByKey(string fieldKey)
    {
        if (string.IsNullOrWhiteSpace(fieldKey))
            return;

        if (!FieldFocusMap.TryGetValue(fieldKey, out var controlName))
            return;

        if (fieldKey == nameof(FirmViewModel.CompositionRate) && !_vm.IsCompositionScheme)
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

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_vm.IsBusy)
        {
            e.Cancel = true;
            return;
        }

        if (!_vm.IsDirty)
            return;

        var shouldClose = _dialogService.Confirm(
            "You have unsaved firm changes. Discard and close?",
            "Close Firm Management");

        if (!shouldClose)
            e.Cancel = true;
    }
}
