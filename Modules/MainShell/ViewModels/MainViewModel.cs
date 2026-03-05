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
using StoreAssistantPro.Modules.Firm.Events;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

public partial class MainViewModel : BaseViewModel
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
    private readonly INotificationService _notificationService;
    private readonly IRegionalSettingsService _regionalSettings;

    // ── Well-known page / dialog keys (defined by each module) ──

    private const string MainWorkspacePage = "MainWorkspace";

    private const string FirmManagementDialog = "FirmManagement";
    private const string UserManagementDialog = "UserManagement";
    private const string TaxManagementDialog = "TaxManagement";
    private const string VendorManagementDialog = "VendorManagement";
    private const string ProductManagementDialog = "ProductManagement";
    private const string CategoryManagementDialog = "CategoryManagement";
    private const string BrandManagementDialog = "BrandManagement";
    private const string InventoryDialog = "Inventory";
    private const string BillingDialog = "Billing";
    private const string FinancialYearDialog = "FinancialYear";
    private const string SystemSettingsDialog = "SystemSettings";
    private const string InwardEntryDialog = "InwardEntry";

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
    public DashboardViewModel DashboardSummary { get; }

    // ── Side panels ──

    [ObservableProperty]
    public partial bool IsNotificationsPanelVisible { get; set; }

    // ── Role-based visibility ──

    public bool IsAdmin => AppState.CurrentUserType == UserType.Admin;

    // ── Quick Action Bar ──

    public ObservableCollection<QuickAction> QuickActions { get; } = [];

    // ── Feature-gated visibility ──

    public bool IsUserManagementEnabled => _features.IsEnabled(FeatureFlags.UserManagement);
    public bool IsFirmManagementEnabled => _features.IsEnabled(FeatureFlags.FirmManagement);

    // ── Combined role + feature visibility (used by menu items) ──

    public bool IsFirmManagementVisible => IsAdmin && IsFirmManagementEnabled;
    public bool IsUserManagementVisible => IsAdmin && IsUserManagementEnabled;
    public bool IsTaxManagementVisible => IsAdmin && _features.IsEnabled(FeatureFlags.TaxManagement);
    public bool IsVendorManagementVisible => IsAdmin && _features.IsEnabled(FeatureFlags.VendorManagement);
    public bool IsProductManagementVisible => IsAdmin && _features.IsEnabled(FeatureFlags.Products);
    public bool IsCategoryManagementVisible => IsAdmin && _features.IsEnabled(FeatureFlags.Products);
    public bool IsBrandManagementVisible => IsAdmin && _features.IsEnabled(FeatureFlags.Products);
    public bool IsFinancialYearVisible => IsAdmin && _features.IsEnabled(FeatureFlags.FinancialYear);
    public bool IsSystemSettingsVisible => IsAdmin && _features.IsEnabled(FeatureFlags.SystemSettings);
    public bool IsInwardEntryVisible => _features.IsEnabled(FeatureFlags.InwardEntry);
    public bool IsInventoryVisible => _features.IsEnabled(FeatureFlags.Inventory);
    public bool IsBillingVisible => _features.IsEnabled(FeatureFlags.Billing);

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
        INotificationService notificationService,
        IRegionalSettingsService regionalSettings,
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
        _notificationService = notificationService;
        _regionalSettings = regionalSettings;
        AppState = appState;
        StatusBar = statusBar;
        DashboardSummary = new DashboardViewModel(appState, eventBus, dashboardService);

        ((ObservableObject)_navigationService).PropertyChanged += OnNavigationPropertyChanged;

        AppState.PropertyChanged += OnAppStatePropertyChanged;
        _features.PropertyChanged += OnFeaturesPropertyChanged;
        _eventBus.Subscribe<FirmUpdatedEvent>(OnFirmUpdatedAsync);
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
        }
    }

    private void OnFeaturesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsUserManagementEnabled));
        OnPropertyChanged(nameof(IsFirmManagementEnabled));
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

    private Task OnDensityChangedAsync(DensityChangedEvent e)
    {
        _navigationService.NavigateTo(_currentPage);
        return Task.CompletedTask;
    }

    private void NotifyCombinedVisibility()
    {
        OnPropertyChanged(nameof(IsFirmManagementVisible));
        OnPropertyChanged(nameof(IsUserManagementVisible));
        OnPropertyChanged(nameof(IsTaxManagementVisible));
        OnPropertyChanged(nameof(IsVendorManagementVisible));
        OnPropertyChanged(nameof(IsProductManagementVisible));
        OnPropertyChanged(nameof(IsCategoryManagementVisible));
        OnPropertyChanged(nameof(IsBrandManagementVisible));
        OnPropertyChanged(nameof(IsFinancialYearVisible));
        OnPropertyChanged(nameof(IsSystemSettingsVisible));
        OnPropertyChanged(nameof(IsInwardEntryVisible));
        OnPropertyChanged(nameof(IsInventoryVisible));
        OnPropertyChanged(nameof(IsBillingVisible));
    }

    // ── Navigation commands ──

    [RelayCommand]
    private void NavigateToMainWorkspace()
    {
        _navigationService.NavigateTo(MainWorkspacePage);
        _currentPage = MainWorkspacePage;
        _statusBar.SetPersistent("Home");
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
        _dialogService.ShowInfo(
            $"{AppState.FirmName}\n\nStore Assistant Pro v1.0.0\n.NET 10 • WPF • EF Core",
            "About");
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
    private void OpenVendorManagement()
    {
        _dialogService.ShowDialog(VendorManagementDialog);
        _statusBar.Post("Vendor management closed");
    }

    [RelayCommand]
    private void OpenProductManagement()
    {
        _dialogService.ShowDialog(ProductManagementDialog);
        _statusBar.Post("Product management closed");
    }

    [RelayCommand]
    private void OpenCategoryManagement()
    {
        _dialogService.ShowDialog(CategoryManagementDialog);
        _statusBar.Post("Category management closed");
    }

    [RelayCommand]
    private void OpenBrandManagement()
    {
        _dialogService.ShowDialog(BrandManagementDialog);
        _statusBar.Post("Brand management closed");
    }

    [RelayCommand]
    private void OpenFinancialYear()
    {
        _dialogService.ShowDialog(FinancialYearDialog);
        _statusBar.Post("Financial year dialog closed");
    }

    [RelayCommand]
    private void OpenSystemSettings()
    {
        _dialogService.ShowDialog(SystemSettingsDialog);
        _statusBar.Post("System settings closed");
    }

    [RelayCommand]
    private void OpenInwardEntry()
    {
        _dialogService.ShowDialog(InwardEntryDialog);
        _statusBar.Post("Inward entry closed");
    }

    [RelayCommand]
    private void OpenInventory()
    {
        _dialogService.ShowDialog(InventoryDialog);
        _statusBar.Post("Inventory management closed");
    }

    [RelayCommand]
    private void OpenBilling()
    {
        _dialogService.ShowDialog(BillingDialog);
        _statusBar.Post("Billing closed");
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
        _regionalSettings.UpdateSettings(e.CurrencySymbol, e.DateFormat);
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
            Title = "Tax", Icon = "💰",
            Description = "Manage GST tax slabs and HSN codes",
            HelpKey = "Tax",
            Command = OpenTaxManagementCommand,
            SortOrder = 55,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.TaxManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Vendors", Icon = "📦",
            Description = "Manage vendor details and GST info",
            HelpKey = "Vendors",
            Command = OpenVendorManagementCommand,
            SortOrder = 60,
            RequiredRoles = [UserType.Admin, UserType.Manager],
            RequiredFeature = FeatureFlags.VendorManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Products", Icon = "👕",
            Description = "Manage product categories and attributes",
            HelpKey = "Products",
            Command = OpenProductManagementCommand,
            SortOrder = 65,
            RequiredRoles = [UserType.Admin, UserType.Manager],
            RequiredFeature = FeatureFlags.Products
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Inward", Icon = "📥",
            Description = "Record new stock inward entries",
            HelpKey = "Inward",
            Command = OpenInwardEntryCommand,
            SortOrder = 70,
            RequiredFeature = FeatureFlags.InwardEntry
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "FY", Icon = "📅",
            Description = "Change financial year and reset billing",
            HelpKey = "FinancialYear",
            Command = OpenFinancialYearCommand,
            SortOrder = 75,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.FinancialYear
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Settings", Icon = "⚙",
            Description = "Backup, restore, and system defaults",
            HelpKey = "Settings",
            Command = OpenSystemSettingsCommand,
            SortOrder = 80,
            RequiredRoles = [UserType.Admin],
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

    public override void Dispose()
    {
        ((ObservableObject)_navigationService).PropertyChanged -= OnNavigationPropertyChanged;
        AppState.PropertyChanged -= OnAppStatePropertyChanged;
        _features.PropertyChanged -= OnFeaturesPropertyChanged;
        _eventBus.Unsubscribe<FirmUpdatedEvent>(OnFirmUpdatedAsync);
        _eventBus.Unsubscribe<DensityChangedEvent>(OnDensityChangedAsync);
        DashboardSummary.Dispose();
        base.Dispose();
    }
}
