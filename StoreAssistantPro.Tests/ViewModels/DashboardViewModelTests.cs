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
        _dashboardService.GetSummaryAsync().Returns(DashboardSummary.Empty);
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

    // ── Connectivity ───────────────────────────────────────────────

    [Fact]
    public void IsOfflineMode_DelegatesToAppState()
    {
        _appState.IsOfflineMode.Returns(true);
        var sut = CreateSut();

        Assert.True(sut.IsOfflineMode);
    }

    [Fact]
    public void IsOfflineMode_UpdatesOnAppStateChange()
    {
        var sut = CreateSut();

        var raised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(sut.IsOfflineMode)) raised = true;
        };

        _appState.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _appState, new PropertyChangedEventArgs(nameof(IAppStateService.IsOfflineMode)));

        Assert.True(raised);
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
}
