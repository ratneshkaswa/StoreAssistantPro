using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.Authentication.Views.SetupPages;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class SetupWindow : Window
{
    private SetupViewModel? _vm;
    private readonly FirmProfilePage _firmPage = new();
    private readonly TaxLegalPage _taxPage = new();
    private readonly RegionalSettingsPage _regionalPage = new();
    private readonly SecuritySettingsPage _securityPage = new();
    private readonly BackupDataPage _backupPage = new();
    private readonly SystemSettingsPage _systemPage = new();

    private readonly IDialogService _dialogService;

    /// <summary>Maps validation field keys to (sidebar section, control name) for focus routing.</summary>
    private static readonly Dictionary<string, (string Section, string Control)> FieldFocusMap = new(StringComparer.Ordinal)
    {
        ["FirmName"] = ("Firm", "FirmNameBox"),
        ["Pincode"] = ("Firm", "PincodeBox"),
        ["Phone"] = ("Firm", "PhoneBox"),
        ["Email"] = ("Firm", "EmailBox"),
        ["GSTIN"] = ("Tax", "GstinBox"),
        ["GSTINChecksum"] = ("Tax", "GstinBox"),
        ["PAN"] = ("Tax", "PanBox"),
        ["CompositionRate"] = ("Tax", "CompRateBox"),
        ["MasterPin"] = ("Security", "MasterPinBox"),
        ["MasterPinConfirm"] = ("Security", "MasterPinBox"),
        ["MasterPinContains"] = ("Security", "MasterPinBox"),
        ["AdminPin"] = ("Security", "AdminPinBox"),
        ["AdminPinConfirm"] = ("Security", "AdminPinBox"),
        ["ManagerPin"] = ("Security", "ManagerPinBox"),
        ["ManagerPinConfirm"] = ("Security", "ManagerPinBox"),
        ["UserPin"] = ("Security", "UserPinBox"),
        ["UserPinConfirm"] = ("Security", "UserPinBox"),
        ["PinConflict"] = ("Security", "AdminPinBox"),
        ["BackupTime"] = ("Backup", "BackupTimeBox"),
        ["BackupLocation"] = ("Backup", "BackupPathBox"),
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

        // Share the ViewModel as DataContext across all pages
        _firmPage.DataContext = vm;
        _taxPage.DataContext = vm;
        _regionalPage.DataContext = vm;
        _securityPage.DataContext = vm;
        _backupPage.DataContext = vm;
        _systemPage.DataContext = vm;

        vm.PropertyChanged += OnViewModelPropertyChanged;

        Closed += (_, _) =>
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.Dispose();
        };

        // Navigate to the default section
        NavigateToSection("Firm");
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not SetupViewModel vm)
            return;

        if (e.PropertyName == nameof(SetupViewModel.FirstErrorFieldKey))
        {
            if (!string.IsNullOrWhiteSpace(vm.FirstErrorFieldKey))
                Dispatcher.BeginInvoke(() => FocusFieldByKey(vm.FirstErrorFieldKey));
        }
        else if (e.PropertyName == nameof(SetupViewModel.SelectedSection))
        {
            Dispatcher.BeginInvoke(() =>
            {
                SyncSidebarSelection();
                NavigateToSection(_vm!.SelectedSection);
            });
        }
    }

    private void FocusFieldByKey(string fieldKey)
    {
        if (FieldFocusMap.TryGetValue(fieldKey, out var target))
            NavigateAndFocus(target.Section, target.Control);
    }

    private void NavigateAndFocus(string section, string controlName)
    {
        if (_vm is null) return;
        _vm.SelectedSection = section;
        NavigateToSection(section);
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            if (ContentFrame.Content is Page page && page.FindName(controlName) is UIElement element)
                element.Focus();
        });
    }

    private void NavigateToSection(string section)
    {
        Page target = section switch
        {
            "Firm" => _firmPage,
            "Tax" => _taxPage,
            "Regional" => _regionalPage,
            "Security" => _securityPage,
            "Backup" => _backupPage,
            "System" => _systemPage,
            _ => _firmPage
        };

        if (ContentFrame.Content != target)
        {
            ContentFrame.Navigate(target);

            // N8: Remove accumulated journal entries to prevent memory growth
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
            {
                if (ContentFrame.NavigationService is { } ns)
                    while (ns.CanGoBack) ns.RemoveBackEntry();
            });
        }

        // Focus the first input field on the target page
        string? firstField = section switch
        {
            "Firm" => "FirmNameBox",
            "Tax" => "GstRegTypeCombo",
            "Regional" => "FyStartCombo",
            "Security" => "MasterPinBox",
            "Backup" => "AutoBackupToggle",
            "System" => "TaxModeCombo",
            _ => null
        };

        if (firstField is not null)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                if (ContentFrame.Content is Page page && page.FindName(firstField) is UIElement element)
                    element.Focus();
            });
        }
    }

    private void OnSidebarSectionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || rb.Tag is not string sectionKey || _vm is null)
            return;

        _vm.SelectedSection = sectionKey;
    }

    private void SyncSidebarSelection()
    {
        if (_vm is null) return;
        RadioButton? target = _vm.SelectedSection switch
        {
            "Firm" => NavFirm,
            "Tax" => NavTax,
            "Regional" => NavRegional,
            "Security" => NavSecurity,
            "Backup" => NavBackup,
            "System" => NavSystem,
            _ => null
        };
        if (target is not null)
            target.IsChecked = true;
    }

    /// <summary>Confirm close if setup is in progress.</summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_vm is null || _vm.IsSetupComplete || DialogResult == true) return;

        // Prevent close during processing
        if (_vm.IsBusy) { e.Cancel = true; return; }

        if (!_dialogService.Confirm("Setup is not complete. Are you sure you want to cancel?", "Cancel Setup"))
            e.Cancel = true;
        else
        {
            _vm.ClearSensitivePins();
            _securityPage.ClearAllPinBoxes();
        }
    }
}