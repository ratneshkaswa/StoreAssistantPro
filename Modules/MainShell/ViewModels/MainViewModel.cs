using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Firm.Events;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

public partial class MainViewModel : BaseViewModel, IDisposable
{
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;
    private readonly IDialogService _dialogService;
    private readonly IWorkflowManager _workflowManager;
    private readonly ICommandBus _commandBus;
    private readonly IEventBus _eventBus;
    private readonly IFeatureToggleService _features;
    private readonly IStatusBarService _statusBar;

    // ── Well-known page / dialog keys (defined by each module) ──

    private const string DashboardPage = "Dashboard";
    private const string ProductsPage = "Products";
    private const string SalesPage = "Sales";

    private const string FirmManagementDialog = "FirmManagement";
    private const string UserManagementDialog = "UserManagement";

    // ── Well-known workflow names ──

    private const string SettingsWorkflow = "Settings";

    // ── Application state (single source of truth) ──

    public IAppStateService AppState { get; }

    // ── Derived display properties ──

    public string WindowTitle => $"{AppState.FirmName} — Store Assistant Pro";

    public string CurrentUserDisplay => $"👤 {AppState.CurrentUserType}";

    // ── Status bar ──

    public IStatusBarService StatusBar { get; }

    // ── Side panels ──

    [ObservableProperty]
    public partial bool IsNotificationsPanelVisible { get; set; }

    [ObservableProperty]
    public partial bool IsTasksPanelVisible { get; set; }

    public bool IsSidePanelVisible => IsNotificationsPanelVisible || IsTasksPanelVisible;

    // ── Role-based visibility ──

    public bool IsAdmin => AppState.CurrentUserType == UserType.Admin;

    // ── Feature-gated visibility ──

    public bool IsProductsEnabled => _features.IsEnabled(FeatureFlags.Products);
    public bool IsSalesEnabled => _features.IsEnabled(FeatureFlags.Sales);
    public bool IsBillingEnabled => _features.IsEnabled(FeatureFlags.Billing);
    public bool IsSystemSettingsEnabled => _features.IsEnabled(FeatureFlags.SystemSettings);
    public bool IsReportsEnabled => _features.IsEnabled(FeatureFlags.Reports);

    // ── Navigation ──

    [ObservableProperty]
    public partial string CurrentPage { get; set; } = DashboardPage;

    public ObservableObject CurrentView => _navigationService.CurrentView;

    // ── Logout ──

    [ObservableProperty]
    public partial bool IsLoggingOut { get; set; }

    public Action? RequestClose { get; set; }

    // ── Constructor ──

    public MainViewModel(
        INavigationService navigationService,
        ISessionService sessionService,
        IDialogService dialogService,
        IAppStateService appState,
        IWorkflowManager workflowManager,
        ICommandBus commandBus,
        IEventBus eventBus,
        IFeatureToggleService features,
        IStatusBarService statusBar)
    {
        _navigationService = navigationService;
        _sessionService = sessionService;
        _dialogService = dialogService;
        _workflowManager = workflowManager;
        _commandBus = commandBus;
        _eventBus = eventBus;
        _features = features;
        _statusBar = statusBar;
        AppState = appState;
        StatusBar = statusBar;

        ((ObservableObject)_navigationService).PropertyChanged += OnNavigationPropertyChanged;

        AppState.PropertyChanged += OnAppStatePropertyChanged;
        _features.PropertyChanged += OnFeaturesPropertyChanged;
        _eventBus.Subscribe<FirmUpdatedEvent>(OnFirmUpdatedAsync);

        _navigationService.NavigateTo(DashboardPage);
    }

    private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentView))
            OnPropertyChanged(nameof(CurrentView));
    }

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IAppStateService.FirmName):
                OnPropertyChanged(nameof(WindowTitle));
                break;
            case nameof(IAppStateService.CurrentUserType):
                OnPropertyChanged(nameof(CurrentUserDisplay));
                OnPropertyChanged(nameof(IsAdmin));
                break;
        }
    }

    private void OnFeaturesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsProductsEnabled));
        OnPropertyChanged(nameof(IsSalesEnabled));
        OnPropertyChanged(nameof(IsBillingEnabled));
        OnPropertyChanged(nameof(IsSystemSettingsEnabled));
        OnPropertyChanged(nameof(IsReportsEnabled));
    }

    // ── Side panel change notifications ──

    partial void OnIsNotificationsPanelVisibleChanged(bool value) =>
        OnPropertyChanged(nameof(IsSidePanelVisible));

    partial void OnIsTasksPanelVisibleChanged(bool value) =>
        OnPropertyChanged(nameof(IsSidePanelVisible));

    // ── Navigation commands ──

    [RelayCommand]
    private void NavigateToDashboard()
    {
        _navigationService.NavigateTo(DashboardPage);
        CurrentPage = DashboardPage;
        _statusBar.SetPersistent("Dashboard");
    }

    [RelayCommand]
    private void NavigateToProducts()
    {
        _navigationService.NavigateTo(ProductsPage);
        CurrentPage = ProductsPage;
        _statusBar.SetPersistent("Products");
    }

    [RelayCommand]
    private void NavigateToSales()
    {
        _navigationService.NavigateTo(SalesPage);
        CurrentPage = SalesPage;
        _statusBar.SetPersistent("Sales");
    }

    // ── Menu commands ──

    [RelayCommand]
    private void RefreshCurrentView()
    {
        _navigationService.NavigateTo(CurrentPage);
        _statusBar.Post("Data refreshed");
    }

    [RelayCommand]
    private void ShowAbout()
    {
        System.Windows.MessageBox.Show(
            $"{AppState.FirmName}\n\nStore Assistant Pro v1.0.0\n.NET 10 • WPF • EF Core",
            "About",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    // ── Management commands (Admin only) ──

    [RelayCommand]
    private void OpenFirmManagement()
    {
        _dialogService.ShowDialog(FirmManagementDialog);
        _statusBar.Post("Firm management closed");
    }

    [RelayCommand]
    private void OpenUserManagement()
    {
        _dialogService.ShowDialog(UserManagementDialog);
        _statusBar.Post("User management closed");
    }

    [RelayCommand]
    private async Task OpenSystemSettingsAsync()
    {
        await _workflowManager.StartWorkflowAsync(SettingsWorkflow);
        _statusBar.Post("Settings closed");
    }

    // ── Logout ──

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var userType = AppState.CurrentUserType;
        await _commandBus.SendAsync(new LogoutCommand(userType));
        IsLoggingOut = true;
        RequestClose?.Invoke();
    }

    // ── Event handlers ──

    private async Task OnFirmUpdatedAsync(FirmUpdatedEvent e)
    {
        await _sessionService.RefreshFirmNameAsync();
        _statusBar.Post($"Firm updated to '{e.FirmName}'");
    }

    // ── Cleanup ──

    public void Dispose()
    {
        ((ObservableObject)_navigationService).PropertyChanged -= OnNavigationPropertyChanged;
        AppState.PropertyChanged -= OnAppStatePropertyChanged;
        _features.PropertyChanged -= OnFeaturesPropertyChanged;
        _eventBus.Unsubscribe<FirmUpdatedEvent>(OnFirmUpdatedAsync);
    }
}
