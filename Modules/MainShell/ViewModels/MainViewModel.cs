using System.Collections.ObjectModel;
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
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Modules.Firm.Events;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.Sales.Events;

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
    private readonly IQuickActionService _quickActionService;
    private readonly IShortcutService _shortcutService;
    private readonly IBillingModeService _billingModeService;
    private readonly IFocusLockService _focusLock;

    // ── Well-known page / dialog keys (defined by each module) ──

    private const string MainWorkspacePage = "MainWorkspace";
    private const string ProductsPage = "Products";
    private const string SalesPage = "Sales";

    private const string FirmManagementDialog = "FirmManagement";
    private const string UserManagementDialog = "UserManagement";
    private const string TaxManagementDialog = "TaxManagement";

    // ── Well-known workflow names ──

    private const string SettingsWorkflow = "Settings";

    // ── Application state (single source of truth) ──

    public IAppStateService AppState { get; }

    // ── Derived display properties ──

    public string WindowTitle => $"{AppState.FirmName} — Store Assistant Pro";

    // ── Status bar ──

    public IStatusBarService StatusBar { get; }

    /// <summary>
    /// Self-contained ViewModel for the status bar summary strip.
    /// Owns current-user display, mode indicator, notification count,
    /// dashboard stats, and clock — all live-updating.
    /// </summary>
    public DashboardSummaryViewModel DashboardSummary { get; }

    // ── Side panels ──

    [ObservableProperty]
    public partial bool IsNotificationsPanelVisible { get; set; }

    [ObservableProperty]
    public partial bool IsTasksPanelVisible { get; set; }

    // ── Role-based visibility ──

    public bool IsAdmin => AppState.CurrentUserType == UserType.Admin;

    // ── Quick Action Bar ──

    public ObservableCollection<QuickAction> QuickActions { get; } = [];

    // ── Feature-gated visibility ──

    public bool IsProductsEnabled => _features.IsEnabled(FeatureFlags.Products);
    public bool IsSalesEnabled => _features.IsEnabled(FeatureFlags.Sales);
    public bool IsBillingEnabled => _features.IsEnabled(FeatureFlags.Billing);
    public bool IsSystemSettingsEnabled => _features.IsEnabled(FeatureFlags.SystemSettings);
    public bool IsReportsEnabled => _features.IsEnabled(FeatureFlags.Reports);
    public bool IsUserManagementEnabled => _features.IsEnabled(FeatureFlags.UserManagement);
    public bool IsFirmManagementEnabled => _features.IsEnabled(FeatureFlags.FirmManagement);
    public bool IsTaxManagementEnabled => _features.IsEnabled(FeatureFlags.TaxManagement);

    // ── Combined role + feature visibility (used by menu/sidebar) ──

    public bool IsFirmManagementVisible => IsAdmin && IsFirmManagementEnabled;
    public bool IsUserManagementVisible => IsAdmin && IsUserManagementEnabled;
    public bool IsTaxManagementVisible => IsAdmin && IsTaxManagementEnabled;

    // ── Billing mode ──

    public bool IsBillingMode => AppState.CurrentMode == OperationalMode.Billing;

    // ── Focus lock (exposed for XAML binding) ──

    public IFocusLockService FocusLock => _focusLock;

    // ── Navigation ──

    [ObservableProperty]
    public partial string CurrentPage { get; set; } = MainWorkspacePage;

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
        IStatusBarService statusBar,
        IQuickActionService quickActionService,
        IShortcutService shortcutService,
        IDashboardService dashboardService,
        IBillingModeService billingModeService,
        IFocusLockService focusLock,
        IEnumerable<IQuickActionContributor> contributors)
    {
        _navigationService = navigationService;
        _sessionService = sessionService;
        _dialogService = dialogService;
        _workflowManager = workflowManager;
        _commandBus = commandBus;
        _eventBus = eventBus;
        _features = features;
        _statusBar = statusBar;
        _quickActionService = quickActionService;
        _shortcutService = shortcutService;
        _billingModeService = billingModeService;
        _focusLock = focusLock;
        AppState = appState;
        StatusBar = statusBar;
        DashboardSummary = new DashboardSummaryViewModel(appState, eventBus, dashboardService);

        ((ObservableObject)_navigationService).PropertyChanged += OnNavigationPropertyChanged;

        AppState.PropertyChanged += OnAppStatePropertyChanged;
        _features.PropertyChanged += OnFeaturesPropertyChanged;
        _eventBus.Subscribe<FirmUpdatedEvent>(OnFirmUpdatedAsync);
        _eventBus.Subscribe<OperationalModeChangedEvent>(OnModeChangedAsync);

        RegisterQuickActions();
        foreach (var contributor in contributors)
            contributor.Contribute(_quickActionService);
        RefreshQuickActions();

        _navigationService.NavigateTo(MainWorkspacePage);
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
                OnPropertyChanged(nameof(IsAdmin));
                NotifyCombinedVisibility();
                RefreshQuickActions();
                break;
            case nameof(IAppStateService.CurrentMode):
                NotifyBillingModeProperties();
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
        OnPropertyChanged(nameof(IsUserManagementEnabled));
        OnPropertyChanged(nameof(IsFirmManagementEnabled));
        OnPropertyChanged(nameof(IsTaxManagementEnabled));
        NotifyCombinedVisibility();
        RefreshQuickActions();
    }

    // ── Side panel change notifications ──

    [RelayCommand]
    private void ToggleNotificationsPanel() =>
        IsNotificationsPanelVisible = !IsNotificationsPanelVisible;

    [RelayCommand]
    private void ToggleTasksPanel() =>
        IsTasksPanelVisible = !IsTasksPanelVisible;

    // ── Billing mode toggle ──

    [RelayCommand]
    private async Task ToggleBillingModeAsync()
    {
        if (IsBillingMode)
            await _billingModeService.StopBillingAsync();
        else
            await _billingModeService.StartBillingAsync();
    }

    private Task OnModeChangedAsync(OperationalModeChangedEvent e)
    {
        NotifyBillingModeProperties();
        RefreshQuickActions();

        // If the current page is no longer available, fall back to Home
        if (!IsPageEnabledForCurrentMode(CurrentPage))
        {
            _navigationService.NavigateTo(MainWorkspacePage);
            CurrentPage = MainWorkspacePage;
        }

        var label = e.NewMode == OperationalMode.Billing ? "Billing" : "Management";
        _statusBar.Post($"Switched to {label} mode");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks whether the given page key is permitted in the current mode
    /// by querying the centralized feature toggle service.
    /// </summary>
    private bool IsPageEnabledForCurrentMode(string pageKey) => pageKey switch
    {
        ProductsPage => _features.IsEnabled(FeatureFlags.Products),
        SalesPage    => _features.IsEnabled(FeatureFlags.Sales),
        _            => true // MainWorkspace and unknown pages are always allowed
    };

    private void NotifyBillingModeProperties()
    {
        OnPropertyChanged(nameof(IsBillingMode));
    }

    private void NotifyCombinedVisibility()
    {
        OnPropertyChanged(nameof(IsFirmManagementVisible));
        OnPropertyChanged(nameof(IsUserManagementVisible));
        OnPropertyChanged(nameof(IsTaxManagementVisible));
    }

    // ── Navigation commands ──

    [RelayCommand]
    private void NavigateToMainWorkspace()
    {
        _navigationService.NavigateTo(MainWorkspacePage);
        CurrentPage = MainWorkspacePage;
        _statusBar.SetPersistent("Home");
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
    private void OpenTaxManagement()
    {
        _dialogService.ShowDialog(TaxManagementDialog);
        _statusBar.Post("Tax management closed");
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

    // ── Quick Action Bar ──

    private void RegisterQuickActions()
    {
        _quickActionService.Register(new QuickAction
        {
            Title = "Home", Icon = "🏠",
            Command = NavigateToMainWorkspaceCommand,
            ShortcutText = "Ctrl+D", Gesture = "Ctrl+D", SortOrder = 0
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "New Bill", Icon = "🧾",
            Command = NavigateToSalesCommand,
            ShortcutText = "Ctrl+S", Gesture = "Ctrl+S", SortOrder = 10,
            RequiredFeature = FeatureFlags.Sales
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Products", Icon = "📦",
            Command = NavigateToProductsCommand,
            ShortcutText = "Ctrl+P", Gesture = "Ctrl+P", SortOrder = 20,
            RequiredFeature = FeatureFlags.Products
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Firm", Icon = "🏢",
            Command = OpenFirmManagementCommand,
            SortOrder = 40,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.FirmManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Users", Icon = "👥",
            Command = OpenUserManagementCommand,
            SortOrder = 50,
            RequiredRoles = [UserType.Admin, UserType.Manager],
            RequiredFeature = FeatureFlags.UserManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Settings", Icon = "⚙️",
            Command = OpenSystemSettingsCommand,
            SortOrder = 80,
            RequiredFeature = FeatureFlags.SystemSettings
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Refresh", Icon = "🔄",
            Command = RefreshCurrentViewCommand,
            ShortcutText = "F5", Gesture = "F5", SortOrder = 90
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Toggle Mode", Icon = "🔀",
            Command = ToggleBillingModeCommand,
            ShortcutText = "F8", Gesture = "F8", SortOrder = 5
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Logout", Icon = "🚪",
            Command = LogoutCommand,
            ShortcutText = "Ctrl+L", Gesture = "Ctrl+L", SortOrder = 100
        });
    }

    private void RefreshQuickActions()
    {
        QuickActions.Clear();
        foreach (var action in _quickActionService.GetVisibleActions(AppState.CurrentUserType, _features))
            QuickActions.Add(action);
    }

    /// <summary>
    /// Syncs <see cref="QuickAction.Gesture"/>-based <c>KeyBinding</c>s
    /// into the host window. Called once by <c>MainWindow</c> code-behind
    /// after <c>DataContext</c> is assigned.
    /// </summary>
    public void ApplyShortcuts(System.Windows.Window window)
    {
        _shortcutService.Apply(window);
    }

    // ── Cleanup ──

    public void Dispose()
    {
        ((ObservableObject)_navigationService).PropertyChanged -= OnNavigationPropertyChanged;
        AppState.PropertyChanged -= OnAppStatePropertyChanged;
        _features.PropertyChanged -= OnFeaturesPropertyChanged;
        _eventBus.Unsubscribe<FirmUpdatedEvent>(OnFirmUpdatedAsync);
        _eventBus.Unsubscribe<OperationalModeChangedEvent>(OnModeChangedAsync);
        DashboardSummary.Dispose();
    }
}
