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
using StoreAssistantPro.Modules.Authentication.ViewModels;
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

    // ── Application state (single source of truth) ──

    public IAppStateService AppState { get; }

    // ── Derived display properties ──

    public string WindowTitle => $"{AppState.FirmName} — Store Assistant Pro";

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

    /// <summary>Raised when Ctrl+F is pressed to focus the search box (#420).</summary>
    public event EventHandler? SearchFocusRequested;

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

    // ── Navigation ──

    /// <summary>Tracks the current page key for mode-change fallback logic.</summary>
    private string _currentPage = LoginPage;

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
        ToastService = toastService;
        DashboardSummary = new DashboardViewModel(appState, eventBus, dashboardService);

        ((ObservableObject)_navigationService).PropertyChanged += OnNavigationPropertyChanged;

        AppState.PropertyChanged += OnAppStatePropertyChanged;
        _features.PropertyChanged += OnFeaturesPropertyChanged;
        _eventBus.Subscribe<FirmUpdatedEvent>(OnFirmUpdatedAsync);

        RegisterQuickActions();
        foreach (var contributor in contributors)
            contributor.Contribute(_quickActionService);
        RefreshQuickActions();

        // Auto-login as User — skip login page entirely to avoid flash
        _ = AutoLoginAsUserAsync();
    }

    /// <summary>
    /// Logs in as User directly through the command bus without ever
    /// showing the login page. Eliminates the startup flash.
    /// </summary>
    private async Task AutoLoginAsUserAsync()
    {
        var result = await _commandBus.SendAsync(
            new LoginUserCommand(UserType.User, string.Empty));

        if (result.Succeeded)
            await OnLoginSucceededAsync(UserType.User);

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

        _navigationService.NavigateTo(MainWorkspacePage);
        _currentPage = MainWorkspacePage;
        _statusBar.SetPersistent(IsBillingVisible ? "Billing ready" : "Workspace");

        RefreshQuickActions();
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

    // ── Shortcut cheat sheet (#414) ──

    [RelayCommand]
    private void ToggleShortcutCheatSheet() =>
        IsShortcutCheatSheetVisible = !IsShortcutCheatSheetVisible;

    /// <summary>Returns all registered shortcuts for the cheat sheet overlay.</summary>
    public IReadOnlyList<ShortcutEntry> GetShortcutEntries()
    {
        return _quickActionService.GetActions()
            .Where(a => !string.IsNullOrWhiteSpace(a.Gesture))
            .OrderBy(a => a.SortOrder)
            .Select(a => new ShortcutEntry(a.ShortcutText, a.Title, a.Description))
            .Append(new ShortcutEntry("F1", "Shortcuts", "Show this shortcut reference"))
            .Append(new ShortcutEntry("Ctrl+F", "Search", "Focus the search box"))
            .ToList();
    }

    // ── Quick search focus (#420) ──

    [RelayCommand]
    private void FocusSearch() => SearchFocusRequested?.Invoke(this, EventArgs.Empty);

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
    }

    // ── Navigation commands ──

    [RelayCommand]
    private void NavigateToMainWorkspace()
    {
        _navigationService.NavigateTo(MainWorkspacePage);
        _currentPage = MainWorkspacePage;
        _statusBar.SetPersistent(IsBillingVisible ? "Billing ready" : "Workspace");
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
        NavigateToPage(FirmManagementPage, "Firm management");
    }

    [RelayCommand]
    private void OpenUserManagement()
    {
        NavigateToPage(UserManagementPage, "User management");
    }

    [RelayCommand]
    private void OpenTaxManagement()
    {
        NavigateToPage(TaxManagementPage, "Tax management");
    }

    [RelayCommand]
    private void OpenVendorManagement()
    {
        NavigateToPage(VendorManagementPage, "Vendor management");
    }

    [RelayCommand]
    private void OpenProductManagement()
    {
        NavigateToPage(ProductManagementPage, "Product management");
    }

    [RelayCommand]
    private void OpenCategoryManagement()
    {
        NavigateToPage(CategoryManagementPage, "Category management");
    }

    [RelayCommand]
    private void OpenBrandManagement()
    {
        NavigateToPage(BrandManagementPage, "Brand management");
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
        NavigateToPage(InventoryPage, "Inventory management");
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
        NavigateToPage(CustomerManagementPage, "Customer management");
    }

    [RelayCommand]
    private void OpenPurchaseOrders()
    {
        NavigateToPage(PurchaseOrdersPage, "Purchase orders");
    }

    [RelayCommand]
    private void OpenExpenseManagement()
    {
        NavigateToPage(ExpenseManagementPage, "Expense management");
    }

    [RelayCommand]
    private void OpenDebtorManagement()
    {
        NavigateToPage(DebtorManagementPage, "Debtor management");
    }

    [RelayCommand]
    private void OpenOrderManagement()
    {
        NavigateToPage(OrderManagementPage, "Order management");
    }

    [RelayCommand]
    private void OpenIroningManagement()
    {
        NavigateToPage(IroningManagementPage, "Ironing management");
    }

    [RelayCommand]
    private void OpenSalaryManagement()
    {
        NavigateToPage(SalaryManagementPage, "Salary management");
    }

    [RelayCommand]
    private void OpenBranchManagement()
    {
        NavigateToPage(BranchManagementPage, "Branch management");
    }

    [RelayCommand]
    private void OpenSalesPurchase()
    {
        NavigateToPage(SalesPurchasePage, "Sales/Purchase register");
    }

    [RelayCommand]
    private void OpenPaymentManagement()
    {
        NavigateToPage(PaymentManagementPage, "Payment management");
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
            _currentPage = LoginPage;
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

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var userType = AppState.CurrentUserType;
        await _commandBus.SendAsync(new LogoutCommand(userType));

        // Navigate back to login page
        _navigationService.NavigateTo(LoginPage);
        _currentPage = LoginPage;
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
            RequiredRoles = [UserType.Admin],
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
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.VendorManagement
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Products", Icon = "👕",
            Description = "Manage product categories and attributes",
            HelpKey = "Products",
            Command = OpenProductManagementCommand,
            SortOrder = 65,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.Products
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Categories", Icon = "🏷",
            Description = "Manage product categories",
            HelpKey = "Categories",
            Command = OpenCategoryManagementCommand,
            SortOrder = 66,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.Products
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Brands", Icon = "🔖",
            Description = "Manage product brands",
            HelpKey = "Brands",
            Command = OpenBrandManagementCommand,
            SortOrder = 67,
            RequiredRoles = [UserType.Admin],
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
            Title = "Inventory", Icon = "📊",
            Description = "Stock adjustments, alerts, and valuation",
            HelpKey = "Inventory",
            Command = OpenInventoryCommand,
            SortOrder = 72,
            RequiredFeature = FeatureFlags.Inventory
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Billing", Icon = "🛒",
            Description = "Create new sales and process payments",
            HelpKey = "Billing",
            Command = OpenBillingCommand,
            SortOrder = 74,
            RequiredFeature = FeatureFlags.Billing
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Sales", Icon = "📋",
            Description = "View sale history and reprint receipts",
            HelpKey = "SaleHistory",
            Command = OpenSaleHistoryCommand,
            SortOrder = 75,
            RequiredFeature = FeatureFlags.Sales
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Customers", Icon = "👤",
            Description = "Manage customer records and contacts",
            HelpKey = "Customers",
            Command = OpenCustomerManagementCommand,
            SortOrder = 76,
            RequiredFeature = FeatureFlags.Customers
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "PO", Icon = "📦",
            Description = "Create and track purchase orders",
            HelpKey = "PurchaseOrders",
            Command = OpenPurchaseOrdersCommand,
            SortOrder = 77,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.PurchaseOrders
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
            SortOrder = 90,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.SystemSettings
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Expenses", Icon = "💸",
            Description = "Track petty cash and daily expenses",
            HelpKey = "Expenses",
            Command = OpenExpenseManagementCommand,
            SortOrder = 78,
            RequiredFeature = FeatureFlags.Expenses
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Debtors", Icon = "📒",
            Description = "Manage debtor accounts and balances",
            HelpKey = "Debtors",
            Command = OpenDebtorManagementCommand,
            SortOrder = 79,
            RequiredFeature = FeatureFlags.Debtors
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Orders", Icon = "📝",
            Description = "Create and track customer orders",
            HelpKey = "Orders",
            Command = OpenOrderManagementCommand,
            SortOrder = 80,
            RequiredFeature = FeatureFlags.Orders
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Ironing", Icon = "👔",
            Description = "Manage ironing batches and entries",
            HelpKey = "Ironing",
            Command = OpenIroningManagementCommand,
            SortOrder = 81,
            RequiredFeature = FeatureFlags.Ironing
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Salaries", Icon = "💰",
            Description = "Record and manage staff salaries",
            HelpKey = "Salaries",
            Command = OpenSalaryManagementCommand,
            SortOrder = 82,
            RequiredRoles = [UserType.Admin],
            RequiredFeature = FeatureFlags.Salaries
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Branch", Icon = "🏬",
            Description = "Track branch bills sent and received",
            HelpKey = "Branch",
            Command = OpenBranchManagementCommand,
            SortOrder = 83,
            RequiredFeature = FeatureFlags.Branch
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Sales/Purchase", Icon = "📊",
            Description = "Sales and purchase register entries",
            HelpKey = "SalesPurchase",
            Command = OpenSalesPurchaseCommand,
            SortOrder = 84,
            RequiredFeature = FeatureFlags.SalesPurchase
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Payments", Icon = "💳",
            Description = "Record customer payments",
            HelpKey = "Payments",
            Command = OpenPaymentManagementCommand,
            SortOrder = 85,
            RequiredFeature = FeatureFlags.Payments
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Reports", Icon = "📈",
            Description = "View expense, order, and financial reports",
            HelpKey = "Reports",
            Command = OpenReportsCommand,
            SortOrder = 86,
            RequiredFeature = FeatureFlags.Reports
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
            Title = "Shortcuts", Icon = "⌨",
            Description = "Show keyboard shortcut reference",
            HelpKey = "Shortcuts",
            Command = ToggleShortcutCheatSheetCommand,
            ShortcutText = "F1", Gesture = "F1", SortOrder = 95,
            IsVisible = false
        });
        _quickActionService.Register(new QuickAction
        {
            Title = "Search", Icon = "🔍",
            Description = "Focus the search box",
            HelpKey = "Search",
            Command = FocusSearchCommand,
            ShortcutText = "Ctrl+F", Gesture = "Ctrl+F", SortOrder = 96,
            IsVisible = false
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
        _currentPage = pageKey;
        _statusBar.SetPersistent(displayName);
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
        RequestClose = null;
        DashboardSummary.Dispose();
        base.Dispose();
    }
}
