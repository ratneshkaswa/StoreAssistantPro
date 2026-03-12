using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.Authentication.Views.SetupPages;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class SetupWindow : Window
{
    private SetupViewModel? _vm;

    private readonly IDialogService _dialogService;

    /// <summary>
    /// Maps validation field keys to (sidebar section, control name) for direct focus routing.
    /// Keep these keys aligned with SetupViewModel.FirstErrorFieldKey values.
    /// </summary>
    private static readonly Dictionary<string, (string Section, string Control)> FieldFocusMap = new(StringComparer.Ordinal)
    {
        ["FirmName"] = ("Firm", "FirmNameBox"),
        ["Address"] = ("Firm", "AddressBox"),
        ["State"] = ("Firm", "StateCombo"),
        ["Pincode"] = ("Firm", "PincodeBox"),
        ["Phone"] = ("Firm", "PhoneBox"),
        ["Email"] = ("Firm", "EmailBox"),

        ["AdminPin"] = ("Security", "AdminPinBox"),
        ["AdminPinConfirm"] = ("Security", "AdminPinConfirmBox"),
        ["UserPin"] = ("Security", "UserPinBox"),
        ["UserPinConfirm"] = ("Security", "UserPinConfirmBox"),
        ["MasterPin"] = ("Security", "MasterPinBox"),
        ["MasterPinConfirm"] = ("Security", "MasterPinConfirmBox"),
        ["MasterPinContains"] = ("Security", "MasterPinBox"),
        ["PinConflict"] = ("Security", "AdminPinBox")
    };

    public SetupWindow(IWindowSizingService sizingService, IDialogService dialogService, SetupViewModel vm)
    {
        InitializeComponent();

        DataContext = _vm = vm;
        _dialogService = dialogService;

        vm.RequestClose = result => DialogResult = result;

        var width = (double)FindResource("SetupWindowWidth");
        var height = (double)FindResource("SetupWindowHeight");
        sizingService.ConfigureStartupWindow(this, width, height);

        SourceInitialized += (_, _) => Win11Backdrop.Apply(this);

        // Share the same ViewModel instance across setup sections.
        FirmProfileSection.DataContext = vm;
        SecuritySettingsSection.DataContext = vm;

        vm.PropertyChanged += OnViewModelPropertyChanged;

        Closed += (_, _) =>
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.Dispose();
        };

        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            TryFocusControl(FirmProfileSection, "FirmNameBox");
        });
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not SetupViewModel vm)
            return;

        if (e.PropertyName == nameof(SetupViewModel.FirstErrorFieldKey))
        {
            if (!string.IsNullOrWhiteSpace(vm.FirstErrorFieldKey))
            {
                Dispatcher.BeginInvoke(() => FocusFieldByKey(vm.FirstErrorFieldKey));
            }
        }
    }

    private void FocusFieldByKey(string fieldKey)
    {
        if (string.IsNullOrWhiteSpace(fieldKey))
            return;

        if (FieldFocusMap.TryGetValue(fieldKey, out var target))
        {
            if (_vm is not null
                && target.Section == "Firm"
                && !string.Equals(fieldKey, "FirmName", StringComparison.Ordinal))
            {
                _vm.ShowOptionalFirmFields = true;
            }

            FrameworkElement page = target.Section == "Security"
                ? SecuritySettingsSection
                : FirmProfileSection;
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                TryFocusControl(page, target.Control);
            });
            return;
        }
    }

    private static void TryFocusControl(FrameworkElement page, string controlName)
    {
        if (page.FindName(controlName) is not UIElement element)
            return;

        if (!element.IsVisible
            && page.FindName(GetVisibleInputFallbackName(controlName)) is UIElement fallbackElement
            && fallbackElement.IsVisible)
        {
            element = fallbackElement;
        }

        switch (element)
        {
            case Control control when !control.IsVisible || !control.IsEnabled:
                return;
            case UIElement uiElement when !uiElement.IsVisible:
                return;
        }

        element.Focus();

        if (element is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private static string GetVisibleInputFallbackName(string controlName) => controlName switch
    {
        "AdminPinBox" => "AdminPinTextBox",
        "AdminPinConfirmBox" => "AdminPinConfirmTextBox",
        "UserPinBox" => "UserPinTextBox",
        "UserPinConfirmBox" => "UserPinConfirmTextBox",
        "MasterPinBox" => "MasterPinTextBox",
        "MasterPinConfirmBox" => "MasterPinConfirmTextBox",
        _ => controlName
    };

    /// <summary>
    /// Confirm close if setup is still in progress.
    /// </summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_vm is null)
            return;

        if (_vm.IsSetupComplete || DialogResult == true)
        {
            _vm.ClearSensitivePins();
            SecuritySettingsSection.ClearAllPinBoxes();
            return;
        }

        if (_vm.IsBusy)
        {
            e.Cancel = true;
            return;
        }

        var shouldClose = _dialogService.Confirm(
            _vm.IsDirty
                ? "You have unsaved setup changes. Discard and close?"
                : "Setup is not complete. Are you sure you want to cancel?",
            "Cancel Setup");

        if (!shouldClose)
        {
            e.Cancel = true;
            return;
        }

        _vm.ClearSensitivePins();
        SecuritySettingsSection.ClearAllPinBoxes();
    }
}
