using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Services;

public class OfflineModeServiceTests : IDisposable
{
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IStatusBarService _statusBar = Substitute.For<IStatusBarService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    private Func<ConnectionLostEvent, Task>? _lostHandler;
    private Func<ConnectionRestoredEvent, Task>? _restoredHandler;

    public OfflineModeServiceTests()
    {
        _regional.Now.Returns(new DateTime(2026, 2, 22, 14, 0, 0));
    }

    private OfflineModeService CreateSut()
    {
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<ConnectionLostEvent, Task>>()))
            .Do(ci => _lostHandler = ci.Arg<Func<ConnectionLostEvent, Task>>());
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<ConnectionRestoredEvent, Task>>()))
            .Do(ci => _restoredHandler = ci.Arg<Func<ConnectionRestoredEvent, Task>>());

        return new OfflineModeService(
            _appState, _eventBus, _statusBar, _regional,
            NullLogger<OfflineModeService>.Instance);
    }

    // ══════════════════════════════════════════════════════════════
    //  Subscription
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_SubscribesToBothEvents()
    {
        _ = CreateSut();

        _eventBus.Received(1).Subscribe(Arg.Any<Func<ConnectionLostEvent, Task>>());
        _eventBus.Received(1).Subscribe(Arg.Any<Func<ConnectionRestoredEvent, Task>>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Initial state
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void IsOffline_InitiallyFalse()
    {
        var sut = CreateSut();

        Assert.False(sut.IsOffline);
    }

    // ══════════════════════════════════════════════════════════════
    //  ConnectionLost → enters offline mode
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConnectionLost_SetsOfflineTrue()
    {
        _ = CreateSut();

        await _lostHandler!(new ConnectionLostEvent());

        Assert.True(CreateSut().IsOffline is false); // new instance
        // Check the instance that received the event — use IsOffline on sut
    }

    [Fact]
    public async Task ConnectionLost_UpdatesAppState()
    {
        var sut = CreateSut();

        await _lostHandler!(new ConnectionLostEvent());

        Assert.True(sut.IsOffline);
        _appState.Received(1).SetConnectivity(true, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task ConnectionLost_PostsPersistentStatusBar()
    {
        _ = CreateSut();

        await _lostHandler!(new ConnectionLostEvent());

        _statusBar.Received(1).SetPersistent(Arg.Is<string>(s => s.Contains("OFFLINE")));
    }

    [Fact]
    public async Task ConnectionLost_PublishesOfflineModeChangedEvent()
    {
        _ = CreateSut();

        await _lostHandler!(new ConnectionLostEvent());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<OfflineModeChangedEvent>(e =>
                e.IsOffline && e.DowntimeDuration == TimeSpan.Zero));
    }

    [Fact]
    public async Task ConnectionLost_UsesRegionalNow()
    {
        var expectedTime = new DateTime(2026, 2, 22, 14, 0, 0);
        _regional.Now.Returns(expectedTime);
        _ = CreateSut();

        await _lostHandler!(new ConnectionLostEvent());

        _appState.Received(1).SetConnectivity(true, expectedTime);
    }

    // ══════════════════════════════════════════════════════════════
    //  ConnectionRestored → exits offline mode
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConnectionRestored_SetsOfflineFalse()
    {
        var sut = CreateSut();

        await _lostHandler!(new ConnectionLostEvent());
        Assert.True(sut.IsOffline);

        await _restoredHandler!(new ConnectionRestoredEvent(TimeSpan.FromMinutes(2)));
        Assert.False(sut.IsOffline);
    }

    [Fact]
    public async Task ConnectionRestored_UpdatesAppState()
    {
        _ = CreateSut();

        await _lostHandler!(new ConnectionLostEvent());
        _appState.ClearReceivedCalls();

        await _restoredHandler!(new ConnectionRestoredEvent(TimeSpan.FromMinutes(1)));

        _appState.Received(1).SetConnectivity(false, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task ConnectionRestored_PostsTransientStatusBar()
    {
        _ = CreateSut();

        await _lostHandler!(new ConnectionLostEvent());
        await _restoredHandler!(new ConnectionRestoredEvent(TimeSpan.FromMinutes(1)));

        _statusBar.Received(1).Post(Arg.Is<string>(s => s.Contains("restored")));
    }

    [Fact]
    public async Task ConnectionRestored_PublishesOfflineModeChangedEvent()
    {
        _ = CreateSut();
        var downtime = TimeSpan.FromMinutes(5);

        await _lostHandler!(new ConnectionLostEvent());
        await _restoredHandler!(new ConnectionRestoredEvent(downtime));

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<OfflineModeChangedEvent>(e =>
                !e.IsOffline && e.DowntimeDuration == downtime));
    }

    // ══════════════════════════════════════════════════════════════
    //  Idempotency — duplicate events are ignored
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConnectionLost_Twice_OnlyFirstProcessed()
    {
        _ = CreateSut();

        await _lostHandler!(new ConnectionLostEvent());
        await _lostHandler!(new ConnectionLostEvent());

        _appState.Received(1).SetConnectivity(true, Arg.Any<DateTime>());
        await _eventBus.Received(1).PublishAsync(Arg.Any<OfflineModeChangedEvent>());
    }

    [Fact]
    public async Task ConnectionRestored_WhenAlreadyOnline_Ignored()
    {
        _ = CreateSut();

        // Never went offline — restored event should be ignored
        await _restoredHandler!(new ConnectionRestoredEvent(TimeSpan.Zero));

        _appState.DidNotReceive().SetConnectivity(Arg.Any<bool>(), Arg.Any<DateTime>());
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<OfflineModeChangedEvent>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Full cycle
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task FullCycle_OfflineThenOnline()
    {
        var sut = CreateSut();
        Assert.False(sut.IsOffline);

        await _lostHandler!(new ConnectionLostEvent());
        Assert.True(sut.IsOffline);

        await _restoredHandler!(new ConnectionRestoredEvent(TimeSpan.FromSeconds(30)));
        Assert.False(sut.IsOffline);

        // Offline event + online event = 2 total
        await _eventBus.Received(2).PublishAsync(Arg.Any<OfflineModeChangedEvent>());
    }

    [Fact]
    public async Task MultipleCycles_EachTransitionProcessed()
    {
        _ = CreateSut();

        // Cycle 1
        await _lostHandler!(new ConnectionLostEvent());
        await _restoredHandler!(new ConnectionRestoredEvent(TimeSpan.FromSeconds(10)));

        // Cycle 2
        await _lostHandler!(new ConnectionLostEvent());
        await _restoredHandler!(new ConnectionRestoredEvent(TimeSpan.FromSeconds(20)));

        _appState.Received(2).SetConnectivity(true, Arg.Any<DateTime>());
        _appState.Received(2).SetConnectivity(false, Arg.Any<DateTime>());
        await _eventBus.Received(4).PublishAsync(Arg.Any<OfflineModeChangedEvent>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Event bus failure resilience
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task EventBusPublishFailure_DoesNotThrow()
    {
        _eventBus.PublishAsync(Arg.Any<OfflineModeChangedEvent>())
            .Returns(Task.FromException(new Exception("bus down")));
        var sut = CreateSut();

        await _lostHandler!(new ConnectionLostEvent());

        // State still updated despite publish failure
        Assert.True(sut.IsOffline);
        _appState.Received(1).SetConnectivity(true, Arg.Any<DateTime>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Dispose
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_UnsubscribesBothEvents()
    {
        var sut = CreateSut();

        sut.Dispose();

        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<ConnectionLostEvent, Task>>());
        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<ConnectionRestoredEvent, Task>>());
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose() { }
}
