using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
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
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.Firm.Events;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.UIPolish.Services;
using StoreAssistantPro.Modules.Users.Services;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private const int MaxRecentCommandPaletteItems = 5;
    private const double QuickActionSlotWidth = 86;
    private const double QuickActionOverflowButtonWidth = 52;
    private static readonly IconService ShellIconService = new();
    private static readonly HashSet<string> QuickAccessHelpKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Refresh",
        "Search",
        "Shortcuts",
        "Logout",
        CommandPaletteHelpKey
    };
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
    private readonly IUserService _userService;
    private readonly List<string> _recentCommandPaletteItemIds = [];

    // ── Well-known page / dialog keys (defined by each module) ──

    private const string LoginPage = "Login";
    private const string MainWorkspacePage = "MainWorkspace";

    private const string FirmManagementPage = "FirmManagement";
    private const string UserManagementPage = "UserManagement";
    private const string TaxManagementPage = "TaxManagement";
    private const string VendorManagementPage = "VendorManagement";
    private const string ProductManagementPage = "ProductManagement";
    private const string CategoryManagementPage = "CategoryManagement";
    private const string BrandManagementPage = "BrandManagement";
    private const string InventoryPage = "Inventory";
    private const string BillingPage = "Billing";
    private const string SaleHistoryPage = "SaleHistory";
    private const string CashRegisterPage = "CashRegister";
    private const string CustomerManagementPage = "CustomerManagement";
    private const string PurchaseOrdersPage = "PurchaseOrders";
    private const string FinancialYearPage = "FinancialYear";
    private const string SystemSettingsPage = "SystemSettings";
    private const string InwardEntryPage = "InwardEntry";
    private const string ExpenseManagementPage = "ExpenseManagement";
    private const string DebtorManagementPage = "DebtorManagement";
    private const string OrderManagementPage = "OrderManagement";
    private const string IroningManagementPage = "IroningManagement";
    private const string SalaryManagementPage = "SalaryManagement";
    private const string BranchManagementPage = "BranchManagement";
    private const string SalesPurchasePage = "SalesPurchase";
    private const string PaymentManagementPage = "PaymentManagement";
    private const string ReportsPage = "Reports";
    private const string BackupRestorePage = "BackupRestore";
    private const string QuotationsPage = "Quotations";
    private const string GRNPage = "GRN";
    private const string BarcodeLabelsPage = "BarcodeLabels";
    private const string CommandPaletteHelpKey = "CommandPalette";

    // ── Application state (single source of truth) ──

    public IAppStateService AppState { get; }

    // ── Derived display properties ──

    public string WindowTitle
    {
        get
        {
            const string appName = "Store Assistant Pro";
            var pageTitle = GetPageDisplayName(_currentPage);
            var firmName = AppState.FirmName;

            if (string.IsNullOrWhiteSpace(firmName))
                return string.IsNullOrWhiteSpace(pageTitle)
                    ? appName
                    : $"{pageTitle} — {appName}";

            return string.IsNullOrWhiteSpace(pageTitle)
                ? $"{firmName} — {appName}"
                : $"{pageTitle} — {firmName} — {appName}";
        }
    }

    // ── Status bar ──

    public IStatusBarService StatusBar { get; }
    public IToastService ToastService { get; }

    /// <summary>
    /// Self-contained ViewModel for the status bar summary strip.
    /// Owns current-user display, mode indicator, notification count,
    /// dashboard stats, and clock — all live-updating.
    /// </summary>
    public DashboardViewModel DashboardSummary { get; }

    // ── Side panels ──

    [ObservableProperty]
    public partial bool IsNotificationsPanelVisible { get; set; }

    [ObservableProperty]
    public partial bool IsReady { get; set; }

    [ObservableProperty]
    public partial bool IsShortcutCheatSheetVisible { get; set; }

    [ObservableProperty]
    public partial bool IsCommandPaletteVisible { get; set; }

    [ObservableProperty]
    public partial string CommandPaletteQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CommandPaletteItem? SelectedCommandPaletteItem { get; set; }

    [ObservableProperty]
    public partial double QuickActionBarViewportWidth { get; set; }

    [ObservableProperty]
    public partial bool IsQuickActionOverflowOpen { get; set; }

    [ObservableProperty]
    public partial bool IsNavigationRailExpanded { get; set; }

    /// <summary>Raised when Ctrl+F is pressed to focus the search box (#420).</summary>
    public event EventHandler? SearchFocusRequested;

    // ── Role-based visibility ──

    public bool IsAdmin => AppState.CurrentUserType == UserType.Admin;

    // ── Quick Action Bar ──

    public ObservableCollection<QuickAction> QuickActions { get; } = [];
    public ObservableCollection<QuickAction> QuickAccessActions { get; } = [];
    public ObservableCollection<CommandPaletteItem> CommandPaletteItems { get; } = [];
    public ObservableCollection<QuickAction> VisibleQuickActions { get; } = [];
    public ObservableCollection<QuickAction> OverflowQuickActions { get; } = [];
    public ObservableCollection<string> ShellBreadcrumbItems { get; } = [];
    public bool HasCommandPaletteItems => CommandPaletteItems.Count > 0;
    public bool HasOverflowQuickActions => OverflowQuickActions.Count > 0;
    public double NavigationRailWidth => IsNavigationRailExpanded ? 320 : 56;
    public bool IsLoginPageActive => string.Equals(_currentPage, LoginPage, StringComparison.Ordinal);
   public bool IsShellChromeVisible => AppState.IsLoggedIn && !IsLoginPageActive;
    public bool IsNavigationRailBackMode => !string.Equals(_currentPage, MainWorkspacePage, StringComparison.Ordinal);

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
    public bool IsSaleHistoryVisible => _features.IsEnabled(FeatureFlags.Sales);
    public bool IsCashRegisterVisible => _features.IsEnabled(FeatureFlags.CashRegister);
    public bool IsCustomerManagementVisible => _features.IsEnabled(FeatureFlags.Customers);
    public bool IsPurchaseOrdersVisible => _features.IsEnabled(FeatureFlags.PurchaseOrders);
    public bool IsExpenseManagementVisible => _features.IsEnabled(FeatureFlags.Expenses);
    public bool IsDebtorManagementVisible => _features.IsEnabled(FeatureFlags.Debtors);
    public bool IsOrderManagementVisible => _features.IsEnabled(FeatureFlags.Orders);
    public bool IsIroningManagementVisible => _features.IsEnabled(FeatureFlags.Ironing);
    public bool IsSalaryManagementVisible => IsAdmin && _features.IsEnabled(FeatureFlags.Salaries);
    public bool IsBranchManagementVisible => _features.IsEnabled(FeatureFlags.Branch);
    public bool IsSalesPurchaseVisible => _features.IsEnabled(FeatureFlags.SalesPurchase);
    public bool IsPaymentManagementVisible => _features.IsEnabled(FeatureFlags.Payments);
    public bool IsReportsVisible => _features.IsEnabled(FeatureFlags.Reports);
    public bool IsBackupRestoreVisible => IsAdmin && _features.IsEnabled(FeatureFlags.Backup);
    public bool IsQuotationsVisible => _features.IsEnabled(FeatureFlags.Quotations);
    public bool IsGRNVisible => _features.IsEnabled(FeatureFlags.GRN);
    public bool IsBarcodeLabelsVisible => _features.IsEnabled(FeatureFlags.Products);

    // ── Navigation ──

    /// <summary>Tracks the current page key for mode-change fallback logic.</summary>
    private string _currentPage = string.Empty;

    public ObservableObject CurrentView => _navigationService.CurrentView;

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
        IToastService toastService,
        IRegionalSettingsService regionalSettings,
        IUserService userService,
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
        _userService = userService;
        AppState = appState;
        StatusBar = statusBar;
        ToastService = toastService;
        DashboardSummary = new DashboardViewModel(appState, eventBus, dashboardService, regionalSettings);
        var preferences = UserPreferencesStore.GetSnapshot();
        IsNavigationRailExpanded = preferences.IsNavigationRailExpanded;
        _recentCommandPaletteItemIds.AddRange(preferences.RecentCommandPaletteItemIds);

        ((ObservableObject)_navigationService).PropertyChanged += OnNavigationPropertyChanged;

        AppState.PropertyChanged += OnAppStatePropertyChanged;
        _features.PropertyChanged += OnFeaturesPropertyChanged;
        _eventBus.Subscribe<FirmUpdatedEvent>(OnFirmUpdatedAsync);

        RegisterQuickActions();
        foreach (var contributor in contributors)
            contributor.Contribute(_quickActionService);
        RefreshQuickActionsCore();

        // Startup now begins in a neutral loading state.
        // This avoids flashing the login screen before direct user auto-login
        // resolves the actual landing page. If auto-login fails, we navigate
        // to login explicitly in the fallback path below.
    }

    internal async Task PrepareStartupAsync()
    {
        if (IsReady)
            return;

        if (AppState.IsInitialSetupPending)
        {
            NavigateToLoginPage();
            IsReady = true;
            return;
        }

        try
        {
            if (!await _userService.HasUserRoleAsync())
            {
                NavigateToLoginPage();
                IsReady = true;
                return;
            }

            var result = await _commandBus.SendAsync(
                new LoginUserCommand(UserType.User, string.Empty));

            if (result.Succeeded)
            {
                await OnLoginSucceededAsync(UserType.User);
                IsReady = true;
                return;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AutoLogin failed: {ex.Message}");
        }

        NavigateToLoginPage();
        IsReady = true;
    }

    /// <summary>
    /// When the current view is a <see cref="LoginViewModel"/>,
    /// subscribe to its <see cref="LoginViewModel.LoginSucceeded"/> callback
    /// so we can transition to the workspace after authentication.
    /// </summary>
    private void WireLoginCallback()
    {
        if (_navigationService.CurrentView is LoginViewModel loginVm)
        {
            loginVm.LoginSucceeded = OnLoginSucceededAsync;
            loginVm.Initialize();
        }
    }

    private async Task OnLoginSucceededAsync(UserType userType)
    {
       await _sessionService.LoginAsync(userType);

       var startupPage = ResolveStartupPage();
       var activationRequest = AppLaunchActivationStore.TryConsumeRequest();
       var destinationPage = string.Equals(startupPage, FirmManagementPage, StringComparison.Ordinal)
           ? startupPage
           : !string.IsNullOrWhiteSpace(activationRequest?.PageKey)
           ? activationRequest.PageKey!
           : startupPage;

       _navigationService.NavigateTo(destinationPage);
       _statusBar.SetPersistent(
           string.Equals(destinationPage, MainWorkspacePage, StringComparison.Ordinal)
               ? (IsBillingVisible ? "Billing ready" : "Workspace")
               : GetPageDisplayName(destinationPage));

       if (activationRequest?.OpenNotificationsRequested == true)
           IsNotificationsPanelVisible = true;

       RefreshQuickActionsCore();
    }

    private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentView))
        {
            if (!string.IsNullOrWhiteSpace(_navigationService.CurrentPageKey))
                SetCurrentPage(_navigationService.CurrentPageKey!);

            OnPropertyChanged(nameof(CurrentView));
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IAppStateService.FirmName):
                OnPropertyChanged(nameof(WindowTitle));
                break;
            case nameof(IAppStateService.IsLoggedIn):
                OnPropertyChanged(nameof(IsShellChromeVisible));
                break;
            case nameof(IAppStateService.CurrentUserType):
                OnPropertyChanged(nameof(IsAdmin));
                NotifyCombinedVisibility();
                RequestQuickActionRefresh();
                break;
        }
    }

    private void OnFeaturesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsUserManagementEnabled));
        OnPropertyChanged(nameof(IsFirmManagementEnabled));
        NotifyCombinedVisibility();
        RequestQuickActionRefresh();
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

    // ── Shortcut cheat sheet (#414) ──

    [RelayCommand]
    private void ToggleShortcutCheatSheet() =>
        IsShortcutCheatSheetVisible = !IsShortcutCheatSheetVisible;

    [RelayCommand]
    private void ToggleCommandPalette() =>
        IsCommandPaletteVisible = !IsCommandPaletteVisible;

    [RelayCommand]
    private void CloseCommandPalette() =>
        IsCommandPaletteVisible = false;

    [RelayCommand]
    private void ToggleQuickActionOverflow() =>
        IsQuickActionOverflowOpen = !IsQuickActionOverflowOpen;

    [RelayCommand]
    private void CloseQuickActionOverflow() =>
        IsQuickActionOverflowOpen = false;

    [RelayCommand]
    private void ToggleNavigationRailOrNavigateBack()
    {
        if (IsNavigationRailBackMode)
        {
            NavigateToMainWorkspace();
            return;
        }

        IsNavigationRailExpanded = !IsNavigationRailExpanded;
    }

    /// <summary>Returns all registered shortcuts for the cheat sheet overlay.</summary>
    public IReadOnlyList<ShortcutEntry> GetShortcutEntries()
    {
        return (_quickActionService.GetActions() ?? [])
            .Where(IsQuickActionAccessible)
            .Where(a => !string.IsNullOrWhiteSpace(a.Gesture))
            .OrderBy(a => a.SortOrder)
            .Select(a => new ShortcutEntry(a.ShortcutText, a.Title, a.Description))
            .DistinctBy(entry => $"{entry.Key}|{entry.Title}", StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ── Quick search focus (#420) ──

    [RelayCommand]
    private void FocusSearch() => SearchFocusRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void SelectNextCommandPaletteItem() => MoveCommandPaletteSelection(1);

    [RelayCommand]
    private void SelectPreviousCommandPaletteItem() => MoveCommandPaletteSelection(-1);

    [RelayCommand]
    private void ExecuteSelectedCommandPaletteItem() =>
        ExecuteCommandPaletteItem(SelectedCommandPaletteItem);

    [RelayCommand]
    private void ExecuteCommandPaletteItem(CommandPaletteItem? item)
    {
        var target = item ?? SelectedCommandPaletteItem ?? CommandPaletteItems.FirstOrDefault();
        if (target?.Action.Command is null || !target.Action.Command.CanExecute(null))
            return;

        RememberCommandPaletteItem(target.Id);
        IsCommandPaletteVisible = false;
        target.Action.Command.Execute(null);
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
        OnPropertyChanged(nameof(IsSaleHistoryVisible));
        OnPropertyChanged(nameof(IsCashRegisterVisible));
        OnPropertyChanged(nameof(IsCustomerManagementVisible));
        OnPropertyChanged(nameof(IsPurchaseOrdersVisible));
        OnPropertyChanged(nameof(IsExpenseManagementVisible));
        OnPropertyChanged(nameof(IsDebtorManagementVisible));
        OnPropertyChanged(nameof(IsOrderManagementVisible));
        OnPropertyChanged(nameof(IsIroningManagementVisible));
        OnPropertyChanged(nameof(IsSalaryManagementVisible));
        OnPropertyChanged(nameof(IsBranchManagementVisible));
        OnPropertyChanged(nameof(IsSalesPurchaseVisible));
        OnPropertyChanged(nameof(IsPaymentManagementVisible));
        OnPropertyChanged(nameof(IsReportsVisible));
        OnPropertyChanged(nameof(IsBackupRestoreVisible));
        OnPropertyChanged(nameof(IsQuotationsVisible));
        OnPropertyChanged(nameof(IsGRNVisible));
        OnPropertyChanged(nameof(IsBarcodeLabelsVisible));
    }

    // ── Navigation commands ──

    [RelayCommand]
    private void NavigateToMainWorkspace()
    {
        _navigationService.NavigateTo(MainWorkspacePage);
        _statusBar.SetPersistent(IsBillingVisible ? "Billing ready" : "Workspace");
    }

    // ── Menu commands ──

    [RelayCommand]
    private void RefreshCurrentView()
    {
        if (string.IsNullOrWhiteSpace(_currentPage))
            return;

        _navigationService.InvalidatePageCache(_currentPage);
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
        NavigateToPage(FirmManagementPage, "Firm");
    }

    [RelayCommand]
    private void OpenUserManagement()
    {
        NavigateToPage(UserManagementPage, "Users");
    }

    [RelayCommand]
    private void OpenTaxManagement()
    {
        NavigateToPage(TaxManagementPage, "Tax");
    }

    [RelayCommand]
    private void OpenVendorManagement()
    {
        NavigateToPage(VendorManagementPage, "Vendors");
    }

    [RelayCommand]
    private void OpenProductManagement()
    {
        NavigateToPage(ProductManagementPage, "Products");
    }

    [RelayCommand]
    private void OpenCategoryManagement()
    {
        NavigateToPage(CategoryManagementPage, "Categories");
    }

    [RelayCommand]
    private void OpenBrandManagement()
    {
        NavigateToPage(BrandManagementPage, "Brands");
    }

    [RelayCommand]
    private void OpenFinancialYear()
    {
        NavigateToPage(FinancialYearPage, "Financial year");
    }

    [RelayCommand]
    private void OpenSystemSettings()
    {
        NavigateToPage(SystemSettingsPage, "System settings");
    }

    [RelayCommand]
    private void OpenInwardEntry()
    {
        NavigateToPage(InwardEntryPage, "Inward entry");
    }

    [RelayCommand]
    private void OpenInventory()
    {
        NavigateToPage(InventoryPage, "Inventory");
    }

    [RelayCommand]
    private void OpenBilling()
    {
        NavigateToPage(BillingPage, "Billing");
    }

    [RelayCommand]
    private void OpenSaleHistory()
    {
        NavigateToPage(SaleHistoryPage, "Sale history");
    }

    [RelayCommand]
    private void OpenCashRegister()
    {
        NavigateToPage(CashRegisterPage, "Cash register");
    }

    [RelayCommand]
    private void OpenCustomerManagement()
    {
        NavigateToPage(CustomerManagementPage, "Customers");
    }

    [RelayCommand]
    private void OpenPurchaseOrders()
    {
        NavigateToPage(PurchaseOrdersPage, "Purchase orders");
    }

    [RelayCommand]
    private void OpenExpenseManagement()
    {
        NavigateToPage(ExpenseManagementPage, "Expenses");
    }

    [RelayCommand]
    private void OpenDebtorManagement()
    {
        NavigateToPage(DebtorManagementPage, "Debtors");
    }

    [RelayCommand]
    private void OpenOrderManagement()
    {
        NavigateToPage(OrderManagementPage, "Orders");
    }

    [RelayCommand]
    private void OpenIroningManagement()
    {
        NavigateToPage(IroningManagementPage, "Ironing");
    }

    [RelayCommand]
    private void OpenSalaryManagement()
    {
        NavigateToPage(SalaryManagementPage, "Salaries");
    }

    [RelayCommand]
    private void OpenBranchManagement()
    {
        NavigateToPage(BranchManagementPage, "Branches");
    }

    [RelayCommand]
    private void OpenSalesPurchase()
    {
        NavigateToPage(SalesPurchasePage, "Sales/Purchase register");
    }

    [RelayCommand]
    private void OpenPaymentManagement()
    {
        NavigateToPage(PaymentManagementPage, "Payments");
    }

    [RelayCommand]
    private void OpenReports()
    {
        NavigateToPage(ReportsPage, "Reports");
    }

    [RelayCommand]
    private void OpenBackupRestore()
    {
        NavigateToPage(BackupRestorePage, "Backup and restore");
    }

    [RelayCommand]
    private void OpenQuotations()
    {
        NavigateToPage(QuotationsPage, "Quotations");
    }

    [RelayCommand]
    private void OpenGRN()
    {
        NavigateToPage(GRNPage, "Goods received notes");
    }

    [RelayCommand]
    private void OpenBarcodeLabels()
    {
        NavigateToPage(BarcodeLabelsPage, "Barcode labels");
    }

    // ── Switch User / Logout ──

    [RelayCommand]
    private async Task SwitchUserAsync()
    {
        var currentRole = AppState.CurrentUserType;

        if (currentRole == UserType.Admin)
        {
            // Admin → User: instant switch — no logout flash
            await AutoLoginAsUserAsync();
        }
        else
        {
            // User → Admin: logout first, then show PIN login
            await _commandBus.SendAsync(new LogoutCommand(currentRole));

            _navigationService.NavigateTo(LoginPage);
            _statusBar.SetPersistent(string.Empty);

            if (_navigationService.CurrentView is LoginViewModel loginVm)
            {
                loginVm.LoginSucceeded = OnLoginSucceededAsync;
                loginVm.IsUserRoleVisible = false;
                loginVm.Initialize();

                // Auto-select Admin so PIN pad shows immediately
                _ = loginVm.SelectUserCommand.ExecuteAsync(UserType.Admin);
            }
        }
    }

    private async Task AutoLoginAsUserAsync()
    {
        if (!await _userService.HasUserRoleAsync())
        {
            NavigateToLoginPage();
            _statusBar.SetPersistent(string.Empty);
            return;
        }

        var currentRole = AppState.CurrentUserType;
        await _commandBus.SendAsync(new LogoutCommand(currentRole));

        var result = await _commandBus.SendAsync(
            new LoginUserCommand(UserType.User, string.Empty));

        if (result.Succeeded)
        {
            await OnLoginSucceededAsync(UserType.User);
            return;
        }

        NavigateToLoginPage();
        _statusBar.SetPersistent(string.Empty);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var userType = AppState.CurrentUserType;
        await _commandBus.SendAsync(new LogoutCommand(userType));

        // Navigate back to login page
        _navigationService.NavigateTo(LoginPage);
        _statusBar.SetPersistent(string.Empty);
        WireLoginCallback();
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
            Title = "Home", Icon = ShellIconService.GetGlyph("Home"),
            Description = "Go to the main dashboard",
            HelpKey = "Home",
            Command = NavigateToMainWorkspaceCommand,
            ShortcutText = "Ctrl+D", Gesture = "Ctrl+D", SortOrder = 0
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Firm", Icon = ShellIconService.GetGlyph("Firm"),
            Description = "Edit firm details and address",
            HelpKey = "Firm",
            Command = OpenFirmManagementCommand,
            SortOrder = 40,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.FirmManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Users", Icon = ShellIconService.GetGlyph("Users"),
            Description = "Manage users, roles, and PINs",
            HelpKey = "Users",
            Command = OpenUserManagementCommand,
            SortOrder = 50,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.UserManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Tax", Icon = ShellIconService.GetGlyph("Tax"),
            Description = "Manage GST tax slabs and HSN codes",
            HelpKey = "Tax",
            Command = OpenTaxManagementCommand,
            SortOrder = 55,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.TaxManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Vendors", Icon = ShellIconService.GetGlyph("Vendors"),
            Description = "Manage vendor details and GST info",
            HelpKey = "Vendors",
            Command = OpenVendorManagementCommand,
            SortOrder = 60,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.VendorManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Products", Icon = ShellIconService.GetGlyph("Products"),
            Description = "Manage product categories and attributes",
            HelpKey = "Products",
            Command = OpenProductManagementCommand,
            SortOrder = 65,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.Products
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Categories", Icon = ShellIconService.GetGlyph("Categories"),
            Description = "Manage product categories",
            HelpKey = "Categories",
            Command = OpenCategoryManagementCommand,
            SortOrder = 66,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.Products
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Brands", Icon = ShellIconService.GetGlyph("Brands"),
            Description = "Manage product brands",
            HelpKey = "Brands",
            Command = OpenBrandManagementCommand,
            SortOrder = 67,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.Products
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Inward", Icon = ShellIconService.GetGlyph("Inward"),
            Description = "Record new stock inward entries",
            HelpKey = "Inward",
            Command = OpenInwardEntryCommand,
            SortOrder = 70,
            RequiredFeature = FeatureFlags.InwardEntry
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Inventory", Icon = ShellIconService.GetGlyph("Inventory"),
            Description = "Stock adjustments, alerts, and valuation",
            HelpKey = "Inventory",
            Command = OpenInventoryCommand,
            SortOrder = 72,
            RequiredFeature = FeatureFlags.Inventory
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Billing", Icon = ShellIconService.GetGlyph("Billing"),
            Description = "Create new sales and process payments",
            HelpKey = "Billing",
            Command = OpenBillingCommand,
            SortOrder = 74,
            RequiredFeature = FeatureFlags.Billing
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Sale History", Icon = ShellIconService.GetGlyph("SaleHistory"),
            Description = "View sale history and reprint receipts",
            HelpKey = "SaleHistory",
            Command = OpenSaleHistoryCommand,
            SortOrder = 75,
            RequiredFeature = FeatureFlags.Sales
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Customers", Icon = ShellIconService.GetGlyph("Customers"),
            Description = "Manage customer records and contacts",
            HelpKey = "Customers",
            Command = OpenCustomerManagementCommand,
            SortOrder = 76,
            RequiredFeature = FeatureFlags.Customers
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Purchase Orders", Icon = ShellIconService.GetGlyph("PurchaseOrders"),
            Description = "Create and track purchase orders",
            HelpKey = "PurchaseOrders",
            Command = OpenPurchaseOrdersCommand,
            SortOrder = 77,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.PurchaseOrders
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Financial Year", Icon = ShellIconService.GetGlyph("FinancialYear"),
            Description = "Change financial year and reset billing",
            HelpKey = "FinancialYear",
            Command = OpenFinancialYearCommand,
            SortOrder = 75,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.FinancialYear
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Settings", Icon = ShellIconService.GetGlyph("Settings"),
            Description = "Backup, restore, and system defaults",
            HelpKey = "Settings",
            Command = OpenSystemSettingsCommand,
            SortOrder = 90,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.SystemSettings
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Expenses", Icon = ShellIconService.GetGlyph("Expenses"),
            Description = "Track petty cash and daily expenses",
            HelpKey = "Expenses",
            Command = OpenExpenseManagementCommand,
            SortOrder = 78,
            RequiredFeature = FeatureFlags.Expenses
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Debtors", Icon = ShellIconService.GetGlyph("Debtors"),
            Description = "Manage debtor accounts and balances",
            HelpKey = "Debtors",
            Command = OpenDebtorManagementCommand,
            SortOrder = 79,
            RequiredFeature = FeatureFlags.Debtors
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Orders", Icon = ShellIconService.GetGlyph("Orders"),
            Description = "Create and track customer orders",
            HelpKey = "Orders",
            Command = OpenOrderManagementCommand,
            SortOrder = 80,
            RequiredFeature = FeatureFlags.Orders
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Ironing", Icon = ShellIconService.GetGlyph("Ironing"),
            Description = "Manage ironing batches and entries",
            HelpKey = "Ironing",
            Command = OpenIroningManagementCommand,
            SortOrder = 81,
            RequiredFeature = FeatureFlags.Ironing
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Salaries", Icon = ShellIconService.GetGlyph("Salaries"),
            Description = "Record and manage staff salaries",
            HelpKey = "Salaries",
            Command = OpenSalaryManagementCommand,
            SortOrder = 82,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.Salaries
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Branches", Icon = ShellIconService.GetGlyph("Branches"),
            Description = "Track branch bills sent and received",
            HelpKey = "Branch",
            Command = OpenBranchManagementCommand,
            SortOrder = 83,
            RequiredFeature = FeatureFlags.Branch
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Sales/Purchase", Icon = ShellIconService.GetGlyph("SalesPurchase"),
            Description = "Sales and purchase register entries",
            HelpKey = "SalesPurchase",
            Command = OpenSalesPurchaseCommand,
            SortOrder = 84,
            RequiredFeature = FeatureFlags.SalesPurchase
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Payments", Icon = ShellIconService.GetGlyph("Payments"),
            Description = "Record customer payments",
            HelpKey = "Payments",
            Command = OpenPaymentManagementCommand,
            SortOrder = 85,
            RequiredFeature = FeatureFlags.Payments
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Reports", Icon = ShellIconService.GetGlyph("Reports"),
            Description = "View expense, order, and financial reports",
            HelpKey = "Reports",
            Command = OpenReportsCommand,
            SortOrder = 86,
            RequiredFeature = FeatureFlags.Reports
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Barcode Labels", Icon = ShellIconService.GetGlyph("BarcodeLabels"),
            Description = "Generate and print barcode labels",
            HelpKey = "BarcodeLabels",
            Command = OpenBarcodeLabelsCommand,
            SortOrder = 87,
            RequiredFeature = FeatureFlags.Products
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Refresh", Icon = ShellIconService.GetGlyph("Refresh"),
            Description = "Reload the current view data",
            HelpKey = "Refresh",
            Command = RefreshCurrentViewCommand,
            ShortcutText = "F5", Gesture = "F5", SortOrder = 90
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Shortcuts", Icon = ShellIconService.GetGlyph("Shortcuts"),
            Description = "Show keyboard shortcut reference",
            HelpKey = "Shortcuts",
            Command = ToggleShortcutCheatSheetCommand,
            ShortcutText = "F1", Gesture = "F1", SortOrder = 95,
            IsVisible = false
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Command Palette", Icon = ShellIconService.GetGlyph("CommandPalette"),
            Description = "Search pages, tools, and recent actions",
            HelpKey = CommandPaletteHelpKey,
            Command = ToggleCommandPaletteCommand,
            ShortcutText = "Ctrl+K", Gesture = "Ctrl+K", SortOrder = 96,
            IsVisible = false
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Search", Icon = ShellIconService.GetGlyph("Search"),
            Description = "Focus the search box",
            HelpKey = "Search",
            Command = FocusSearchCommand,
            ShortcutText = "Ctrl+F", Gesture = "Ctrl+F", SortOrder = 97,
            IsVisible = false
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Logout", Icon = ShellIconService.GetGlyph("Logout"),
            Description = "Sign out and return to the login screen",
            HelpKey = "Logout",
            Command = LogoutCommand,
            ShortcutText = "Ctrl+L", Gesture = "Ctrl+L", SortOrder = 100
        });
    }

    private void NavigateToLoginPage()
    {
        try
        {
            _navigationService.NavigateTo(LoginPage);
            WireLoginCallback();
        }
        catch
        {
            // Keep the shell alive even if login navigation fails.
        }
    }

    private void RequestQuickActionRefresh()
    {
        RefreshQuickActionsCore();
    }

    private void RefreshQuickActionsCore()
    {
        QuickActions.Clear();
        foreach (var action in (_quickActionService.GetVisibleActions(AppState.CurrentUserType, _features) ?? [])
                     .Where(ShouldShowInNavigationRail))
            QuickActions.Add(action);

        QuickAccessActions.Clear();
        foreach (var action in (_quickActionService.GetActions() ?? [])
                     .Where(IsQuickActionAccessible)
                     .Where(ShouldShowInQuickAccessBar)
                     .OrderBy(action => action.SortOrder))
        {
            QuickAccessActions.Add(action);
        }

        UpdateQuickActionActiveState();
        RecomputeQuickActionOverflow();

        if (IsCommandPaletteVisible)
            RequestCommandPaletteRefresh();
    }

    partial void OnCommandPaletteQueryChanged(string value) =>
        RequestCommandPaletteRefresh();

    partial void OnQuickActionBarViewportWidthChanged(double value) =>
        RecomputeQuickActionOverflow();

    partial void OnIsCommandPaletteVisibleChanged(bool value)
    {
        if (!value)
        {
            if (!string.IsNullOrEmpty(CommandPaletteQuery))
                CommandPaletteQuery = string.Empty;

            CommandPaletteItems.Clear();
            SelectedCommandPaletteItem = null;
            OnPropertyChanged(nameof(HasCommandPaletteItems));
            return;
        }

        if (!string.IsNullOrEmpty(CommandPaletteQuery))
            CommandPaletteQuery = string.Empty;

        RequestCommandPaletteRefresh();
    }

    private void RequestCommandPaletteRefresh() => RefreshCommandPaletteItemsCore();

    private void RefreshCommandPaletteItemsCore()
    {
        if (!IsCommandPaletteVisible)
        {
            CommandPaletteItems.Clear();
            SelectedCommandPaletteItem = null;
            OnPropertyChanged(nameof(HasCommandPaletteItems));
            return;
        }

        var query = CommandPaletteQuery.Trim();
        IEnumerable<CommandPaletteItem> items = (_quickActionService.GetActions() ?? [])
            .Where(IsQuickActionAccessible)
            .Where(action => !string.Equals(GetCommandPaletteItemId(action), CommandPaletteHelpKey, StringComparison.OrdinalIgnoreCase))
            .Select(CreateCommandPaletteItem);

        items = string.IsNullOrWhiteSpace(query)
            ? items
                .OrderByDescending(item => item.IsRecent)
                .ThenBy(item => item.SortOrder)
            : items
                .Where(item => MatchesCommandPaletteQuery(item, query))
                .OrderBy(item => GetCommandPaletteMatchRank(item, query))
                .ThenByDescending(item => item.IsRecent)
                .ThenBy(item => item.SortOrder);

        CommandPaletteItems.Clear();
        foreach (var item in items)
            CommandPaletteItems.Add(item);

        SelectedCommandPaletteItem = CommandPaletteItems.FirstOrDefault();
        OnPropertyChanged(nameof(HasCommandPaletteItems));
    }

    private void RecomputeQuickActionOverflow()
    {
        VisibleQuickActions.Clear();
        OverflowQuickActions.Clear();

        var actions = QuickAccessActions.ToList();
        if (actions.Count == 0)
        {
            IsQuickActionOverflowOpen = false;
            OnPropertyChanged(nameof(HasOverflowQuickActions));
            return;
        }

        if (QuickActionBarViewportWidth <= 0)
        {
            foreach (var action in actions)
                VisibleQuickActions.Add(action);

            IsQuickActionOverflowOpen = false;
            OnPropertyChanged(nameof(HasOverflowQuickActions));
            return;
        }

        var visibleCount = (int)Math.Floor(QuickActionBarViewportWidth / QuickActionSlotWidth);
        if (actions.Count > visibleCount)
        {
            var reducedWidth = Math.Max(
                QuickActionBarViewportWidth - QuickActionOverflowButtonWidth,
                QuickActionSlotWidth);
            visibleCount = Math.Max(1, (int)Math.Floor(reducedWidth / QuickActionSlotWidth));
        }

        visibleCount = Math.Clamp(visibleCount, 1, actions.Count);

        foreach (var action in actions.Take(visibleCount))
            VisibleQuickActions.Add(action);

        foreach (var action in actions.Skip(visibleCount))
            OverflowQuickActions.Add(action);

        if (OverflowQuickActions.Count == 0)
            IsQuickActionOverflowOpen = false;

        OnPropertyChanged(nameof(HasOverflowQuickActions));
    }

    private bool IsQuickActionAccessible(QuickAction action)
    {
        if (action.RequiredRoles.Count > 0 && !action.RequiredRoles.Contains(AppState.CurrentUserType))
            return false;

        if (action.RequiredFeature is not null && !_features.IsEnabled(action.RequiredFeature))
            return false;

        return action.Command is not null;
    }

    private static bool ShouldShowInQuickAccessBar(QuickAction action) =>
        !string.IsNullOrWhiteSpace(action.HelpKey) &&
        QuickAccessHelpKeys.Contains(action.HelpKey);

    private static bool ShouldShowInNavigationRail(QuickAction action) =>
        !ShouldShowInQuickAccessBar(action);

    private CommandPaletteItem CreateCommandPaletteItem(QuickAction action)
    {
        var id = GetCommandPaletteItemId(action);
        return new CommandPaletteItem(
            id,
            action.Title,
            string.IsNullOrWhiteSpace(action.Description) ? "No description available." : action.Description,
            action.Icon,
            action.ShortcutText,
            GetCommandPaletteCategory(action),
            action.SortOrder,
            IsRecentCommandPaletteItem(id),
            action);
    }

    private void MoveCommandPaletteSelection(int direction)
    {
        if (CommandPaletteItems.Count == 0)
            return;

        if (SelectedCommandPaletteItem is null)
        {
            SelectedCommandPaletteItem = direction >= 0
                ? CommandPaletteItems[0]
                : CommandPaletteItems[^1];
            return;
        }

        var currentIndex = CommandPaletteItems.IndexOf(SelectedCommandPaletteItem);
        if (currentIndex < 0)
        {
            SelectedCommandPaletteItem = CommandPaletteItems[0];
            return;
        }

        var nextIndex = (currentIndex + direction + CommandPaletteItems.Count) % CommandPaletteItems.Count;
        SelectedCommandPaletteItem = CommandPaletteItems[nextIndex];
    }

    private void RememberCommandPaletteItem(string id)
    {
        _recentCommandPaletteItemIds.RemoveAll(existing =>
            string.Equals(existing, id, StringComparison.OrdinalIgnoreCase));

        _recentCommandPaletteItemIds.Insert(0, id);
        if (_recentCommandPaletteItemIds.Count > MaxRecentCommandPaletteItems)
        {
            _recentCommandPaletteItemIds.RemoveRange(
                MaxRecentCommandPaletteItems,
                _recentCommandPaletteItemIds.Count - MaxRecentCommandPaletteItems);
        }

        UserPreferencesStore.Update(state =>
            state.RecentCommandPaletteItemIds = [.. _recentCommandPaletteItemIds]);
    }

    private bool IsRecentCommandPaletteItem(string id) =>
        _recentCommandPaletteItemIds.Any(existing =>
            string.Equals(existing, id, StringComparison.OrdinalIgnoreCase));

    private static string GetCommandPaletteItemId(QuickAction action) =>
        action.HelpKey ?? action.Title;

    private static bool MatchesCommandPaletteQuery(CommandPaletteItem item, string query)
    {
        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return true;

        return tokens.All(token =>
            item.Title.Contains(token, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(token, StringComparison.OrdinalIgnoreCase)
            || item.Category.Contains(token, StringComparison.OrdinalIgnoreCase)
            || item.ShortcutText.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static int GetCommandPaletteMatchRank(CommandPaletteItem item, string query)
    {
        if (item.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 0;

        if (item.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 1;

        if (item.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 2;

        if (item.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 3;

        if (item.ShortcutText.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 4;

        return 5;
    }

    private static string GetCommandPaletteCategory(QuickAction action) =>
        (action.HelpKey ?? action.Title) switch
        {
            "Home" or "Search" or "Shortcuts" or CommandPaletteHelpKey or "Refresh" or "Logout" => "Shell",
            "Billing" or "SaleHistory" or "CashRegister" or "Customers" or "Orders" or "Payments" or "Debtors" or "Reports" or "Quotations" or "SalesPurchase" => "Sales",
            "Products" or "Categories" or "Brands" or "Tax" or "Vendors" or "Inventory" or "Inward" or "PurchaseOrders" or "GRN" or "BarcodeLabels" => "Inventory",
            "Settings" or "Backup" or "FinancialYear" or "Firm" or "Users" => "Administration",
            "Expenses" or "Salaries" or "Branch" or "Ironing" => "Operations",
            _ => "Actions"
        };

    private void OpenDialog(string dialogKey, string displayName, string? closedStatus = null)
    {
        if (_dialogService.ShowDialog(dialogKey) == false)
        {
            _statusBar.Post($"Unable to open {displayName}");
            return;
        }

        if (!string.IsNullOrWhiteSpace(closedStatus))
            _statusBar.Post(closedStatus);
    }

    private void NavigateToPage(string pageKey, string displayName)
    {
        _navigationService.NavigateTo(pageKey);
        _statusBar.SetPersistent(displayName);
    }

    private void SetCurrentPage(string pageKey)
    {
        _currentPage = pageKey;
        PersistCurrentPage(pageKey);
        UpdateQuickActionActiveState();
        UpdateShellBreadcrumbs();
        OnPropertyChanged(nameof(IsLoginPageActive));
        OnPropertyChanged(nameof(IsShellChromeVisible));
        OnPropertyChanged(nameof(IsNavigationRailBackMode));
        OnPropertyChanged(nameof(WindowTitle));
    }

    partial void OnIsNavigationRailExpandedChanged(bool value)
    {
        UserPreferencesStore.Update(state => state.IsNavigationRailExpanded = value);
        OnPropertyChanged(nameof(NavigationRailWidth));
    }

    [RelayCommand]
    private void ActivateBreadcrumb(string? crumb)
    {
        if (string.IsNullOrWhiteSpace(crumb))
            return;

        if (string.Equals(crumb, "Home", StringComparison.OrdinalIgnoreCase))
        {
            NavigateToMainWorkspace();
        }
    }

    private void UpdateQuickActionActiveState()
    {
        var activeHelpKey = GetActiveQuickActionHelpKey(_currentPage);
        foreach (var action in _quickActionService.GetActions() ?? [])
        {
            action.IsActive = !string.IsNullOrWhiteSpace(activeHelpKey) &&
                              string.Equals(action.HelpKey, activeHelpKey, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void UpdateShellBreadcrumbs()
    {
        ShellBreadcrumbItems.Clear();
        foreach (var item in GetPageBreadcrumbItems(_currentPage))
            ShellBreadcrumbItems.Add(item);
    }

    private static string? GetActiveQuickActionHelpKey(string? pageKey) => pageKey switch
    {
        MainWorkspacePage => "Home",
        FirmManagementPage => "Firm",
        UserManagementPage => "Users",
        TaxManagementPage => "Tax",
        VendorManagementPage => "Vendors",
        ProductManagementPage => "Products",
        CategoryManagementPage => "Categories",
        BrandManagementPage => "Brands",
        InventoryPage => "Inventory",
        BillingPage => "Billing",
        SaleHistoryPage => "SaleHistory",
        CustomerManagementPage => "Customers",
        PurchaseOrdersPage => "PurchaseOrders",
        FinancialYearPage => "FinancialYear",
        SystemSettingsPage => "Settings",
        InwardEntryPage => "Inward",
        ExpenseManagementPage => "Expenses",
        DebtorManagementPage => "Debtors",
        OrderManagementPage => "Orders",
        IroningManagementPage => "Ironing",
        SalaryManagementPage => "Salaries",
        BranchManagementPage => "Branch",
        SalesPurchasePage => "SalesPurchase",
        PaymentManagementPage => "Payments",
        ReportsPage => "Reports",
        BackupRestorePage => "Backup",
        QuotationsPage => "Quotations",
        GRNPage => "GRN",
        BarcodeLabelsPage => "BarcodeLabels",
        _ => null
    };

    private static string GetPageDisplayName(string? pageKey) => pageKey switch
    {
        LoginPage => "Sign in",
        MainWorkspacePage => "Home",
        FirmManagementPage => "Firm",
        UserManagementPage => "Users",
        TaxManagementPage => "Tax",
        VendorManagementPage => "Vendors",
        ProductManagementPage => "Products",
        CategoryManagementPage => "Categories",
        BrandManagementPage => "Brands",
        InventoryPage => "Inventory",
        BillingPage => "Billing",
        SaleHistoryPage => "Sale history",
        CashRegisterPage => "Cash register",
        CustomerManagementPage => "Customers",
        PurchaseOrdersPage => "Purchase orders",
        FinancialYearPage => "Financial year",
        SystemSettingsPage => "System settings",
        InwardEntryPage => "Inward entry",
        ExpenseManagementPage => "Expenses",
        DebtorManagementPage => "Debtors",
        OrderManagementPage => "Orders",
        IroningManagementPage => "Ironing",
        SalaryManagementPage => "Salaries",
        BranchManagementPage => "Branches",
        SalesPurchasePage => "Sales/Purchase register",
        PaymentManagementPage => "Payments",
        ReportsPage => "Reports",
        BackupRestorePage => "Backup and restore",
        QuotationsPage => "Quotations",
        GRNPage => "Goods received notes",
        BarcodeLabelsPage => "Barcode labels",
        _ => string.Empty
    };

    private static IReadOnlyList<string> GetPageBreadcrumbItems(string? pageKey) => pageKey switch
    {
        LoginPage => [],
        MainWorkspacePage => ["Home"],
        BillingPage or SaleHistoryPage or CashRegisterPage or CustomerManagementPage or OrderManagementPage
            or PaymentManagementPage or DebtorManagementPage or ReportsPage or QuotationsPage or SalesPurchasePage
            => ["Home", "Sales", GetPageDisplayName(pageKey)],
        VendorManagementPage or ProductManagementPage or CategoryManagementPage or BrandManagementPage
            or InventoryPage or PurchaseOrdersPage or InwardEntryPage or GRNPage or BarcodeLabelsPage or TaxManagementPage
            => ["Home", "Inventory", GetPageDisplayName(pageKey)],
        FirmManagementPage or UserManagementPage or FinancialYearPage or SystemSettingsPage or BackupRestorePage
            => ["Home", "Administration", GetPageDisplayName(pageKey)],
        ExpenseManagementPage or IroningManagementPage or SalaryManagementPage or BranchManagementPage
            => ["Home", "Operations", GetPageDisplayName(pageKey)],
        _ => ["Home", GetPageDisplayName(pageKey)]
    };

    private void PersistCurrentPage(string pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey) ||
            string.Equals(pageKey, LoginPage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        UserPreferencesStore.Update(state => state.LastVisitedPage = pageKey);
    }

    private string ResolveStartupPage()
    {
        if (AppState.IsInitialSetupPending && IsFirmManagementVisible)
            return FirmManagementPage;

        var preferences = UserPreferencesStore.GetSnapshot();
        var pageKey = preferences.LastVisitedPage;

        if (!preferences.RestoreLastVisitedPageOnLogin ||
            string.IsNullOrWhiteSpace(pageKey) ||
            !IsRestorablePageAccessible(pageKey))
        {
            return MainWorkspacePage;
        }

        return pageKey;
    }

    private bool IsRestorablePageAccessible(string? pageKey) => pageKey switch
    {
        MainWorkspacePage => true,
        FirmManagementPage => IsFirmManagementVisible,
        UserManagementPage => IsUserManagementVisible,
        TaxManagementPage => IsTaxManagementVisible,
        VendorManagementPage => IsVendorManagementVisible,
        ProductManagementPage => IsProductManagementVisible,
        CategoryManagementPage => IsCategoryManagementVisible,
        BrandManagementPage => IsBrandManagementVisible,
        InventoryPage => IsInventoryVisible,
        BillingPage => IsBillingVisible,
        SaleHistoryPage => IsSaleHistoryVisible,
        CashRegisterPage => IsCashRegisterVisible,
        CustomerManagementPage => IsCustomerManagementVisible,
        PurchaseOrdersPage => IsPurchaseOrdersVisible,
        FinancialYearPage => IsFinancialYearVisible,
        SystemSettingsPage => IsSystemSettingsVisible,
        InwardEntryPage => IsInwardEntryVisible,
        ExpenseManagementPage => IsExpenseManagementVisible,
        DebtorManagementPage => IsDebtorManagementVisible,
        OrderManagementPage => IsOrderManagementVisible,
        IroningManagementPage => IsIroningManagementVisible,
        SalaryManagementPage => IsSalaryManagementVisible,
        BranchManagementPage => IsBranchManagementVisible,
        SalesPurchasePage => IsSalesPurchaseVisible,
        PaymentManagementPage => IsPaymentManagementVisible,
        ReportsPage => IsReportsVisible,
        BackupRestorePage => IsBackupRestoreVisible,
        QuotationsPage => IsQuotationsVisible,
        GRNPage => IsGRNVisible,
        BarcodeLabelsPage => IsBarcodeLabelsVisible,
        _ => false
    };

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
        RequestClose = null;
        DashboardSummary.Dispose();
        base.Dispose();
    }
}
