using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.Sales.Events;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

/// <summary>
/// Self-contained ViewModel for the status bar's dashboard summary strip.
/// Subscribes to <see cref="IAppStateService"/> property changes and
/// <see cref="IEventBus"/> events so every metric updates live without
/// manual refresh calls from <see cref="MainViewModel"/>.
/// <para>
/// <b>Architecture rule:</b> The status bar binds to
/// <c>MainViewModel.DashboardSummary.*</c>. No status-bar state lives
/// directly on <see cref="MainViewModel"/>.
/// </para>
/// </summary>
public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IAppStateService _appState;
    private readonly IEventBus _eventBus;
    private readonly IDashboardService _dashboardService;

    public DashboardViewModel(
        IAppStateService appState,
        IEventBus eventBus,
        IDashboardService dashboardService)
    {
        _appState = appState;
        _eventBus = eventBus;
        _dashboardService = dashboardService;

        _appState.PropertyChanged += OnAppStatePropertyChanged;
        _eventBus.Subscribe<SaleCompletedEvent>(OnSaleCompletedAsync);
        _eventBus.Subscribe<OperationalModeChangedEvent>(OnModeChangedAsync);

        _ = RefreshStatsAsync();
    }

    // ── Current user ──

    public string CurrentUser => $"👤 {_appState.CurrentUserType}";
    public UserType CurrentUserType => _appState.CurrentUserType;

    // ── Operational mode ──

    public bool IsBillingMode => _appState.CurrentMode == OperationalMode.Billing;
    public string ModeDisplay => IsBillingMode ? "BILLING" : "MANAGEMENT";

    // ── Clock ──

    public string CurrentTime => _appState.CurrentTime;

    // ── Notifications ──

    public int NotificationCount => _appState.UnreadNotificationCount;

    // ── Connectivity ──

    public bool IsOfflineMode => _appState.IsOfflineMode;
    public string ConnectionStatusDisplay => IsOfflineMode ? "OFFLINE" : "ONLINE";

    // ── Active bills (future placeholder) ──

    [ObservableProperty]
    public partial int ActiveBillCount { get; set; }

    // ── Dashboard stats ──

    [ObservableProperty]
    public partial int ProductCount { get; set; }

    [ObservableProperty]
    public partial int LowStockCount { get; set; }

    [ObservableProperty]
    public partial int OutOfStockCount { get; set; }

    [ObservableProperty]
    public partial decimal InventoryValue { get; set; }

    [ObservableProperty]
    public partial decimal TodaysSales { get; set; }

    [ObservableProperty]
    public partial int TodaysTransactions { get; set; }

    // ── Live update wiring ──

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IAppStateService.CurrentUserType):
                OnPropertyChanged(nameof(CurrentUser));
                OnPropertyChanged(nameof(CurrentUserType));
                break;
            case nameof(IAppStateService.CurrentMode):
                OnPropertyChanged(nameof(IsBillingMode));
                OnPropertyChanged(nameof(ModeDisplay));
                break;
            case nameof(IAppStateService.CurrentTime):
                OnPropertyChanged(nameof(CurrentTime));
                break;
            case nameof(IAppStateService.UnreadNotificationCount):
                OnPropertyChanged(nameof(NotificationCount));
                break;
            case nameof(IAppStateService.IsOfflineMode):
                OnPropertyChanged(nameof(IsOfflineMode));
                OnPropertyChanged(nameof(ConnectionStatusDisplay));
                break;
        }
    }

    private Task OnModeChangedAsync(OperationalModeChangedEvent e)
    {
        OnPropertyChanged(nameof(IsBillingMode));
        OnPropertyChanged(nameof(ModeDisplay));
        return Task.CompletedTask;
    }

    private async Task OnSaleCompletedAsync(SaleCompletedEvent e)
    {
        await RefreshStatsAsync();
    }

    public async Task RefreshStatsAsync()
    {
        try
        {
            var summary = await _dashboardService.GetSummaryAsync();
            ProductCount = summary.TotalProducts;
            LowStockCount = summary.LowStockCount;
            OutOfStockCount = summary.OutOfStockCount;
            InventoryValue = summary.InventoryValue;
            TodaysSales = summary.TodaysSales;
            TodaysTransactions = summary.TodaysTransactions;
        }
        catch
        {
            // Status bar stats are non-critical; swallow errors silently.
        }
    }

    // ── Cleanup ──

    public void Dispose()
    {
        _appState.PropertyChanged -= OnAppStatePropertyChanged;
        _eventBus.Unsubscribe<SaleCompletedEvent>(OnSaleCompletedAsync);
        _eventBus.Unsubscribe<OperationalModeChangedEvent>(OnModeChangedAsync);
    }
}
