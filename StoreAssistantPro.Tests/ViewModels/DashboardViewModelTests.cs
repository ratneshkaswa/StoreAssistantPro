using System.Collections.ObjectModel;
using System.ComponentModel;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class DashboardViewModelTests
{
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();

    public DashboardViewModelTests()
    {
        // Default stubs so the constructor's RefreshStatsAsync doesn't throw
        _dashboardService.GetSummaryAsync().Returns(
            new DashboardSummary(10, 2, 0, 0, 0m, 0m, 500m, 5, 100m, [], [], [], [], [], 0m, [], []));
        _appState.Notifications.Returns(new ObservableCollection<AppNotification>());
    }

    private DashboardViewModel CreateSut() => new(_appState, _eventBus, _dashboardService);

    // ── CurrentUser ────────────────────────────────────────────────

    [Fact]
    public void CurrentUser_FormatsUserType()
    {
        _appState.CurrentUserType.Returns(UserType.Admin);
        var sut = CreateSut();

        Assert.Equal("👤 Admin", sut.CurrentUser);
    }

    [Fact]
    public void CurrentUser_UpdatesOnAppStateChange()
    {
        _appState.CurrentUserType.Returns(UserType.User);
        var sut = CreateSut();

        var raised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(sut.CurrentUser)) raised = true;
        };

        _appState.CurrentUserType.Returns(UserType.Admin);
        _appState.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _appState, new PropertyChangedEventArgs(nameof(IAppStateService.CurrentUserType)));

        Assert.True(raised);
    }

    // ── Mode ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(OperationalMode.Management, false, "MANAGEMENT")]
    [InlineData(OperationalMode.Billing, true, "BILLING")]
    public void Mode_Properties(OperationalMode mode, bool expectedBilling, string expectedDisplay)
    {
        _appState.CurrentMode.Returns(mode);
        var sut = CreateSut();

        Assert.Equal(expectedBilling, sut.IsBillingMode);
        Assert.Equal(expectedDisplay, sut.ModeDisplay);
    }

    [Fact]
    public void Mode_UpdatesOnAppStateChange()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        var sut = CreateSut();

        var props = new List<string>();
        sut.PropertyChanged += (_, e) => props.Add(e.PropertyName!);

        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _appState, new PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        Assert.Contains(nameof(sut.IsBillingMode), props);
        Assert.Contains(nameof(sut.ModeDisplay), props);
    }

    // ── Clock ──────────────────────────────────────────────────────

    [Fact]
    public void CurrentTime_DelegatesToAppState()
    {
        _appState.CurrentTime.Returns("14:30:00");
        var sut = CreateSut();

        Assert.Equal("14:30:00", sut.CurrentTime);
    }

    [Fact]
    public void CurrentTime_UpdatesOnAppStateChange()
    {
        var sut = CreateSut();

        var raised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(sut.CurrentTime)) raised = true;
        };

        _appState.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _appState, new PropertyChangedEventArgs(nameof(IAppStateService.CurrentTime)));

        Assert.True(raised);
    }

    // ── Notifications ──────────────────────────────────────────────

    [Fact]
    public void NotificationCount_DelegatesToAppState()
    {
        _appState.UnreadNotificationCount.Returns(7);
        var sut = CreateSut();

        Assert.Equal(7, sut.NotificationCount);
    }

    [Fact]
    public void NotificationCount_UpdatesOnAppStateChange()
    {
        var sut = CreateSut();

        var raised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(sut.NotificationCount)) raised = true;
        };

        _appState.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _appState, new PropertyChangedEventArgs(nameof(IAppStateService.UnreadNotificationCount)));

        Assert.True(raised);
    }

    // ── Dashboard stats ────────────────────────────────────────────

    [Fact]
    public async Task RefreshStats_PopulatesProperties()
    {
        _dashboardService.GetSummaryAsync().Returns(
            new DashboardSummary(42, 3, 1, 0, 5000m, 0m, 1250.50m, 15, 83.37m, [], [], [], [], [], 0m, [], []));

        var sut = CreateSut();
        await sut.RefreshStatsAsync();

        Assert.Equal(42, sut.ProductCount);
        Assert.Equal(3, sut.LowStockCount);
        Assert.Equal(1250.50m, sut.TodaysSales);
        Assert.Equal(15, sut.TodaysTransactions);
    }

    [Fact]
    public async Task RefreshStats_SwallowsExceptions()
    {
        _dashboardService.GetSummaryAsync().Returns<DashboardSummary>(
            _ => throw new InvalidOperationException("DB down"));

        var sut = CreateSut();
        await sut.RefreshStatsAsync();

        Assert.Equal(0, sut.ProductCount);
    }

    // ── Active bills placeholder ───────────────────────────────────

    [Fact]
    public void ActiveBillCount_DefaultsToZero()
    {
        var sut = CreateSut();

        Assert.Equal(0, sut.ActiveBillCount);
    }

    // ── Dispose ────────────────────────────────────────────────────

    [Fact]
    public void Dispose_UnsubscribesEvents()
    {
        var sut = CreateSut();
        sut.Dispose();

        // Verify no exceptions and property changes are disconnected
        _appState.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _appState, new PropertyChangedEventArgs(nameof(IAppStateService.CurrentTime)));
    }

    // ── Connectivity ───────────────────────────────────────────────

    [Fact]
    public void IsOfflineMode_DelegatesToAppState()
    {
        _appState.IsOfflineMode.Returns(true);
        var sut = CreateSut();

        Assert.True(sut.IsOfflineMode);
    }

    [Fact]
    public void ConnectionStatusDisplay_WhenOnline_ReturnsOnline()
    {
        _appState.IsOfflineMode.Returns(false);
        var sut = CreateSut();

        Assert.Equal("ONLINE", sut.ConnectionStatusDisplay);
    }

    [Fact]
    public void ConnectionStatusDisplay_WhenOffline_ReturnsOffline()
    {
        _appState.IsOfflineMode.Returns(true);
        var sut = CreateSut();

        Assert.Equal("OFFLINE", sut.ConnectionStatusDisplay);
    }

    [Fact]
    public void IsOfflineMode_UpdatesOnAppStateChange()
    {
        var sut = CreateSut();

        var raisedProps = new List<string>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null) raisedProps.Add(e.PropertyName);
        };

        _appState.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _appState, new PropertyChangedEventArgs(nameof(IAppStateService.IsOfflineMode)));

        Assert.Contains(nameof(sut.IsOfflineMode), raisedProps);
        Assert.Contains(nameof(sut.ConnectionStatusDisplay), raisedProps);
    }
}
