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

    public SetupWindow(IWindowSizingService sizingService, SetupViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        vm.RequestClose = result => DialogResult = result;

        sizingService.ConfigureStartupWindow(this, 960, 720);

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

        if (e.PropertyName == nameof(SetupViewModel.ErrorMessage))
        {
            if (!string.IsNullOrWhiteSpace(vm.ErrorMessage))
                Dispatcher.BeginInvoke(() => FocusFirstInvalidField(vm.ErrorMessage));
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

    private void FocusFirstInvalidField(string error)
    {
        if (error.Contains("Firm name", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Firm", "FirmNameBox"); return; }
        if (error.Contains("Admin PIN", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Security", "AdminPinBox"); return; }
        if (error.Contains("Manager PIN", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Security", "ManagerPinBox"); return; }
        if (error.Contains("User PIN", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Security", "UserPinBox"); return; }
        if (error.Contains("Master PIN", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Security", "MasterPinBox"); return; }
        if (error.Contains("unique PIN", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Security", "AdminPinBox"); return; }
        if (error.Contains("GSTIN", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Tax", "GstinBox"); return; }
        if (error.Contains("PAN", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Tax", "PanBox"); return; }
        if (error.Contains("Composition rate", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Tax", "CompRateBox"); return; }
        if (error.Contains("Pincode", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Firm", "PincodeBox"); return; }
        if (error.Contains("Email", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Firm", "EmailBox"); return; }
        if (error.Contains("Phone", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Firm", "PhoneBox"); return; }
        if (error.Contains("Backup time", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Backup", "BackupTimeBox"); return; }
        if (error.Contains("Backup location", StringComparison.OrdinalIgnoreCase)) { NavigateAndFocus("Backup", "BackupPathBox"); return; }
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
            ContentFrame.Navigate(target);

        // Focus the first input field on the target page
        string? firstField = section switch
        {
            "Firm" => "FirmNameBox",
            "Tax" => "GstRegTypeCombo",
            "Security" => "MasterPinBox",
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

        var result = MessageBox.Show(
            "Setup is not complete. Are you sure you want to cancel?",
            "Cancel Setup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
            e.Cancel = true;
        else
            _vm.ClearSensitivePins();
    }
}