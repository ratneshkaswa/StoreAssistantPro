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
    private bool _suppressSectionAutoFocus;

    private readonly FirmProfilePage _firmPage = new();
    private readonly SecuritySettingsPage _securityPage = new();

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
        ["ManagerPin"] = ("Security", "ManagerPinBox"),
        ["ManagerPinConfirm"] = ("Security", "ManagerPinConfirmBox"),
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
        vm.UseEssentialSetupValidationOnly = true;
        _dialogService = dialogService;

        vm.RequestClose = result => DialogResult = result;

        var width = (double)FindResource("SetupWindowWidth");
        var height = (double)FindResource("SetupWindowHeight");
        sizingService.ConfigureStartupWindow(this, width, height);

        SourceInitialized += (_, _) => Win11Backdrop.Apply(this);

        // Share the same ViewModel instance across all setup pages.
        _firmPage.DataContext = vm;
        _securityPage.DataContext = vm;

        vm.PropertyChanged += OnViewModelPropertyChanged;

        Closed += (_, _) =>
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.Dispose();
        };

        NavigateToSection("Firm", focusFirstField: true);
        SyncSidebarSelection();
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
        else if (e.PropertyName == nameof(SetupViewModel.SelectedSection))
        {
            Dispatcher.BeginInvoke(() =>
            {
                SyncSidebarSelection();
                NavigateToSection(vm.SelectedSection, focusFirstField: !_suppressSectionAutoFocus);
            });
        }
    }

    private void FocusFieldByKey(string fieldKey)
    {
        if (string.IsNullOrWhiteSpace(fieldKey))
            return;

        if (FieldFocusMap.TryGetValue(fieldKey, out var target))
        {
            NavigateAndFocus(target.Section, target.Control);
            return;
        }

        // Safe fallback: just stay on current section if key is unknown.
        if (_vm is not null)
            NavigateToSection(_vm.SelectedSection, focusFirstField: true);
    }

    private void NavigateAndFocus(string section, string controlName)
    {
        if (_vm is null)
            return;

        _suppressSectionAutoFocus = true;

        if (!string.Equals(_vm.SelectedSection, section, StringComparison.Ordinal))
            _vm.SelectedSection = section;

        NavigateToSection(section, focusFirstField: false);

        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            TryFocusControl(controlName);
            _suppressSectionAutoFocus = false;
        });
    }

    private void NavigateToSection(string section, bool focusFirstField)
    {
        Page target = section switch
        {
            "Firm" => _firmPage,
            "Security" => _securityPage,
            _ => _firmPage
        };

        if (!ReferenceEquals(ContentFrame.Content, target))
        {
            ContentFrame.Navigate(target);

            // Keep frame journal empty to avoid unnecessary back-stack growth.
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                if (ContentFrame.NavigationService is { } ns)
                {
                    while (ns.CanGoBack)
                        ns.RemoveBackEntry();
                }
            });
        }

        var firstField = section switch
        {
            "Firm" => "FirmNameBox",
            "Security" => "AdminPinBox",
            _ => null
        };

        if (focusFirstField && !string.IsNullOrWhiteSpace(firstField))
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                TryFocusControl(firstField);
            });
        }
    }

    private void TryFocusControl(string controlName)
    {
        if (ContentFrame.Content is not Page page)
            return;

        if (page.FindName(controlName) is not UIElement element)
            return;

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

    private void OnSidebarSectionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || rb.Tag is not string sectionKey || _vm is null)
            return;

        if (!string.Equals(_vm.SelectedSection, sectionKey, StringComparison.Ordinal))
            _vm.SelectedSection = sectionKey;
    }

    private void SyncSidebarSelection()
    {
        if (_vm is null)
            return;

        var target = _vm.SelectedSection switch
        {
            "Firm" => NavFirm,
            "Security" => NavSecurity,
            _ => null
        };

        if (target is not null && target.IsChecked != true)
            target.IsChecked = true;
    }

    /// <summary>
    /// Confirm close if setup is still in progress.
    /// </summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_vm is null || _vm.IsSetupComplete || DialogResult == true)
            return;

        if (_vm.IsBusy)
        {
            e.Cancel = true;
            return;
        }

        var shouldClose = _dialogService.Confirm(
            "Setup is not complete. Are you sure you want to cancel?",
            "Cancel Setup");

        if (!shouldClose)
        {
            e.Cancel = true;
            return;
        }

        _vm.ClearSensitivePins();
        _securityPage.ClearAllPinBoxes();
    }
}
