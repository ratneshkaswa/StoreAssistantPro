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
    private readonly INotificationService _notificationService;

    // ── Well-known page / dialog keys (defined by each module) ──

    private const string MainWorkspacePage = "MainWorkspace";
    private const string ProductsPage = "Products";
    private const string BrandsPage = "Brands";
    private const string SalesPage = "Sales";
    private const string SuppliersPage = "Suppliers";

    private const string FirmManagementDialog = "FirmManagement";
    private const string UserManagementDialog = "UserManagement";
    private const string TaxManagementDialog = "TaxManagement";
    private const string TasksDialog = "Tasks";

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

    // ── Combined role + feature visibility (used by menu items) ──

    public bool IsFirmManagementVisible => IsAdmin && IsFirmManagementEnabled;
    public bool IsUserManagementVisible => IsAdmin && IsUserManagementEnabled;
    public bool IsTaxManagementVisible => IsAdmin && IsTaxManagementEnabled;

    // ── Billing mode ──

    public bool IsBillingMode => AppState.CurrentMode == OperationalMode.Billing;

    // ── Focus lock (exposed for XAML binding) ──

    public IFocusLockService FocusLock => _focusLock;

    // ── Navigation ──

    /// <summary>Tracks the current page key for mode-change fallback logic.</summary>
    private string _currentPage = MainWorkspacePage;

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
        INotificationService notificationService,
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
        _notificationService = notificationService;
        AppState = appState;
        StatusBar = statusBar;
        DashboardSummary = new DashboardSummaryViewModel(appState, eventBus, dashboardService);

        ((ObservableObject)_navigationService).PropertyChanged += OnNavigationPropertyChanged;

        AppState.PropertyChanged += OnAppStatePropertyChanged;
        _features.PropertyChanged += OnFeaturesPropertyChanged;
        _eventBus.Subscribe<FirmUpdatedEvent>(OnFirmUpdatedAsync);
        _eventBus.Subscribe<OperationalModeChangedEvent>(OnModeChangedAsync);
        _eventBus.Subscribe<SaleCompletedEvent>(OnSaleCompletedAsync);
        _eventBus.Subscribe<DensityChangedEvent>(OnDensityChangedAsync);

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

    // ── Notification popup ──

    [RelayCommand]
    private void ToggleNotificationsPanel() =>
        IsNotificationsPanelVisible = !IsNotificationsPanelVisible;

    [RelayCommand]
    private async Task MarkAllNotificationsReadAsync()
    {
        await _notificationService.MarkAllReadAsync();
    }

    /// <summary>2d: Click a single notification to mark it as read.</summary>
    [RelayCommand]
    private async Task MarkNotificationReadAsync(AppNotification? notification)
    {
        if (notification is not null)
            await _notificationService.MarkReadAsync(notification);
    }

    [RelayCommand]
    private async Task ClearNotificationsAsync()
    {
        await _notificationService.ClearAsync();
        IsNotificationsPanelVisible = false;
    }

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
        if (!IsPageEnabledForCurrentMode(_currentPage))
        {
            _navigationService.NavigateTo(MainWorkspacePage);
            _currentPage = MainWorkspacePage;
        }

        var label = e.NewMode == OperationalMode.Billing ? "Billing" : "Management";
        _statusBar.Post($"Switched to {label} mode");
        return Task.CompletedTask;
    }

    private Task OnDensityChangedAsync(DensityChangedEvent e)
    {
        // Re-navigate to refresh layout with new density tokens
        _navigationService.NavigateTo(_currentPage);
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
        _currentPage = MainWorkspacePage;
        _statusBar.SetPersistent("Home");
    }

    [RelayCommand]
    private void NavigateToProducts()
    {
        _navigationService.NavigateTo(ProductsPage);
        _currentPage = ProductsPage;
        _statusBar.SetPersistent("Products");
    }

    [RelayCommand]
    private void NavigateToBrands()
    {
        _navigationService.NavigateTo(BrandsPage);
        _currentPage = BrandsPage;
        _statusBar.SetPersistent("Brands");
    }

    [RelayCommand]
    private void NavigateToSales()
    {
        _navigationService.NavigateTo(SalesPage);
        _currentPage = SalesPage;
        _statusBar.SetPersistent("Sales");
    }

    // ── Menu commands ──

    [RelayCommand]
    private void RefreshCurrentView()
    {
        _navigationService.NavigateTo(_currentPage);
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
    private void OpenTasks()
    {
        _dialogService.ShowDialog(TasksDialog);
        _statusBar.Post("Tasks closed");
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

    private async Task OnSaleCompletedAsync(SaleCompletedEvent e)
    {
        await _notificationService.PostAsync(
            "Sale Completed",
            $"Sale #{e.SaleId} — {e.TotalAmount:C} recorded successfully.",
            AppNotificationLevel.Success);
    }

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
            Description = "Go to the main dashboard",
            HelpKey = "Home",
            Command = NavigateToMainWorkspaceCommand,
            ShortcutText = "Ctrl+D", Gesture = "Ctrl+D", SortOrder = 0
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "New Bill", Icon = "🧾",
            Description = "Start a new billing session",
            HelpKey = "NewBill",
            Command = NavigateToSalesCommand,
            ShortcutText = "Ctrl+S", Gesture = "Ctrl+S", SortOrder = 10,
            RequiredFeature = FeatureFlags.Sales
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Products", Icon = "📦",
            Description = "Manage product catalog and inventory",
            HelpKey = "Products",
            Command = NavigateToProductsCommand,
            ShortcutText = "Ctrl+P", Gesture = "Ctrl+P", SortOrder = 20,
            RequiredFeature = FeatureFlags.Products
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Firm", Icon = "🏢",
            Description = "Edit firm details and address",
            HelpKey = "Firm",
            Command = OpenFirmManagementCommand,
            SortOrder = 40,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.FirmManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Users", Icon = "👥",
            Description = "Manage users, roles, and PINs",
            HelpKey = "Users",
            Command = OpenUserManagementCommand,
            SortOrder = 50,
            RequiredRoles = [UserType.Admin, UserType.Manager],
            RequiredFeature = FeatureFlags.UserManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Tax", Icon = "💲",
            Description = "Configure tax rates and rules",
            HelpKey = "Tax",
            Command = OpenTaxManagementCommand,
            SortOrder = 60,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.TaxManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Tasks", Icon = "📋",
            Description = "View pending tasks and reminders",
            HelpKey = "Tasks",
            Command = OpenTasksCommand,
            ShortcutText = "F6", Gesture = "F6", SortOrder = 70
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Settings", Icon = "⚙️",
            Description = "Open system settings and preferences",
            HelpKey = "Settings",
            Command = OpenSystemSettingsCommand,
            SortOrder = 80,
            RequiredFeature = FeatureFlags.SystemSettings
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Refresh", Icon = "🔄",
            Description = "Reload the current view data",
            HelpKey = "Refresh",
            Command = RefreshCurrentViewCommand,
            ShortcutText = "F5", Gesture = "F5", SortOrder = 90
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Toggle Mode", Icon = "🔀",
            Description = "Switch between management and billing mode",
            HelpKey = "ToggleMode",
            Command = ToggleBillingModeCommand,
            ShortcutText = "F8", Gesture = "F8", SortOrder = 5
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Logout", Icon = "🚪",
            Description = "Sign out and return to the login screen",
            HelpKey = "Logout",
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
