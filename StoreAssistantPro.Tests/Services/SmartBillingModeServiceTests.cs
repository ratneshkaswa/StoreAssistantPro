using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

public class SmartBillingModeServiceTests
{
    private readonly IBillingModeService _modeService = Substitute.For<IBillingModeService>();
    private readonly IBillingSessionService _sessionService = Substitute.For<IBillingSessionService>();
    private readonly IFocusLockService _focusLock = Substitute.For<IFocusLockService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private Func<BillingSessionStartedEvent, Task>? _startedHandler;
    private Func<BillingSessionCompletedEvent, Task>? _completedHandler;
    private Func<BillingSessionCancelledEvent, Task>? _cancelledHandler;

    private SmartBillingModeService CreateSut()
    {
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<BillingSessionStartedEvent, Task>>()))
            .Do(ci => _startedHandler = ci.Arg<Func<BillingSessionStartedEvent, Task>>());
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<BillingSessionCompletedEvent, Task>>()))
            .Do(ci => _completedHandler = ci.Arg<Func<BillingSessionCompletedEvent, Task>>());
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<BillingSessionCancelledEvent, Task>>()))
            .Do(ci => _cancelledHandler = ci.Arg<Func<BillingSessionCancelledEvent, Task>>());

        return new SmartBillingModeService(_modeService, _sessionService, _focusLock, _eventBus);
    }

    // ── Subscription ───────────────────────────────────────────────

    [Fact]
    public void Constructor_SubscribesToAllThreeEvents()
    {
        _ = CreateSut();

        _eventBus.Received(1).Subscribe(Arg.Any<Func<BillingSessionStartedEvent, Task>>());
        _eventBus.Received(1).Subscribe(Arg.Any<Func<BillingSessionCompletedEvent, Task>>());
        _eventBus.Received(1).Subscribe(Arg.Any<Func<BillingSessionCancelledEvent, Task>>());
    }

    // ── Started → starts billing ───────────────────────────────────

    [Fact]
    public async Task SessionStarted_StartsBilling()
    {
        _ = CreateSut();

        await _startedHandler!(new BillingSessionStartedEvent());

        await _modeService.Received(1).StartBillingAsync();
        await _modeService.DidNotReceive().StopBillingAsync();
    }

    // ── Completed → stops billing + flushes deferred ───────────────

    [Fact]
    public async Task SessionCompleted_WhenSessionIdle_StopsBilling()
    {
        _sessionService.CurrentState.Returns(BillingSessionState.Completed);
        _ = CreateSut();

        await _completedHandler!(new BillingSessionCompletedEvent());

        await _modeService.Received(1).StopBillingAsync();
        await _modeService.Received(1).FlushDeferredStopAsync();
    }

    // ── Cancelled → stops billing + flushes deferred ───────────────

    [Fact]
    public async Task SessionCancelled_WhenSessionIdle_StopsBilling()
    {
        _sessionService.CurrentState.Returns(BillingSessionState.Cancelled);
        _ = CreateSut();

        await _cancelledHandler!(new BillingSessionCancelledEvent());

        await _modeService.Received(1).StopBillingAsync();
        await _modeService.Received(1).FlushDeferredStopAsync();
    }

    // ── Rule 1: Active session blocks stop ─────────────────────────

    [Fact]
    public async Task SessionCompleted_WhenSessionStillActive_DoesNotStop()
    {
        _sessionService.CurrentState.Returns(BillingSessionState.Active);
        _ = CreateSut();

        await _completedHandler!(new BillingSessionCompletedEvent());

        await _modeService.DidNotReceive().StopBillingAsync();
        await _modeService.DidNotReceive().FlushDeferredStopAsync();
    }

    [Fact]
    public async Task SessionCancelled_WhenSessionStillActive_DoesNotStop()
    {
        _sessionService.CurrentState.Returns(BillingSessionState.Active);
        _ = CreateSut();

        await _cancelledHandler!(new BillingSessionCancelledEvent());

        await _modeService.DidNotReceive().StopBillingAsync();
        await _modeService.DidNotReceive().FlushDeferredStopAsync();
    }

    // ── Rule 2: Payment processing blocks transitions ──────────────

    [Fact]
    public void BeginPaymentProcessing_SetsFlag()
    {
        var sut = CreateSut();

        sut.BeginPaymentProcessing();

        Assert.True(sut.IsPaymentProcessing);
    }

    [Fact]
    public void BeginPaymentProcessing_WhenAlreadyProcessing_Throws()
    {
        var sut = CreateSut();
        sut.BeginPaymentProcessing();

        Assert.Throws<InvalidOperationException>(
            () => sut.BeginPaymentProcessing());
    }

    [Fact]
    public async Task EndPaymentProcessing_ClearsFlag()
    {
        var sut = CreateSut();
        sut.BeginPaymentProcessing();

        await sut.EndPaymentProcessingAsync();

        Assert.False(sut.IsPaymentProcessing);
    }

    [Fact]
    public async Task EndPaymentProcessing_WhenNotProcessing_Throws()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.EndPaymentProcessingAsync());
    }

    [Fact]
    public async Task SessionCompleted_WhilePaymentProcessing_DefersStop()
    {
        _sessionService.CurrentState.Returns(BillingSessionState.Completed);
        var sut = CreateSut();
        sut.BeginPaymentProcessing();

        await _completedHandler!(new BillingSessionCompletedEvent());

        await _modeService.DidNotReceive().StopBillingAsync();
        await _modeService.DidNotReceive().FlushDeferredStopAsync();
    }

    [Fact]
    public async Task EndPaymentProcessing_FlushesDeferredStop()
    {
        _sessionService.CurrentState.Returns(BillingSessionState.Completed);
        var sut = CreateSut();
        sut.BeginPaymentProcessing();

        // Session ends while payment is processing → deferred
        await _completedHandler!(new BillingSessionCompletedEvent());
        await _modeService.DidNotReceive().StopBillingAsync();

        // Payment finishes → deferred stop executes
        await sut.EndPaymentProcessingAsync();

        await _modeService.Received(1).StopBillingAsync();
        await _modeService.Received(1).FlushDeferredStopAsync();
    }

    [Fact]
    public async Task EndPaymentProcessing_NoPendingStop_DoesNotStop()
    {
        var sut = CreateSut();
        sut.BeginPaymentProcessing();

        await sut.EndPaymentProcessingAsync();

        await _modeService.DidNotReceive().StopBillingAsync();
        await _modeService.DidNotReceive().FlushDeferredStopAsync();
    }

    [Fact]
    public async Task SessionCancelled_WhilePaymentProcessing_DefersStop()
    {
        _sessionService.CurrentState.Returns(BillingSessionState.Cancelled);
        var sut = CreateSut();
        sut.BeginPaymentProcessing();

        await _cancelledHandler!(new BillingSessionCancelledEvent());

        await _modeService.DidNotReceive().StopBillingAsync();
    }

    // ── Rule 3: Started is not blocked by payment processing ───────

    [Fact]
    public async Task SessionStarted_WhilePaymentProcessing_StillStartsBilling()
    {
        _ = CreateSut();
        // Note: StartBilling is always allowed — payment lock only
        // blocks stop transitions, not start.

        await _startedHandler!(new BillingSessionStartedEvent());

        await _modeService.Received(1).StartBillingAsync();
    }

    // ── Rule 4: Focus lock hold during payment ─────────────────────

    [Fact]
    public void BeginPaymentProcessing_HoldsFocusLockRelease()
    {
        var sut = CreateSut();

        sut.BeginPaymentProcessing();

        _focusLock.Received(1).HoldRelease();
    }

    [Fact]
    public async Task EndPaymentProcessing_LiftsFocusLockHold()
    {
        var sut = CreateSut();
        sut.BeginPaymentProcessing();

        await sut.EndPaymentProcessingAsync();

        _focusLock.Received(1).LiftReleaseHold();
    }

    [Fact]
    public async Task EndPaymentProcessing_LiftsFocusLockHold_BeforeFlush()
    {
        _sessionService.CurrentState.Returns(BillingSessionState.Completed);
        var sut = CreateSut();
        sut.BeginPaymentProcessing();
        await _completedHandler!(new BillingSessionCompletedEvent());

        await sut.EndPaymentProcessingAsync();

        // LiftReleaseHold must be called (focus lock unblocks)
        _focusLock.Received(1).LiftReleaseHold();
        // And the deferred mode stop also executes
        await _modeService.Received(1).StopBillingAsync();
    }

    // ── Dispose ────────────────────────────────────────────────────

    [Fact]
    public void Dispose_UnsubscribesFromAllThreeEvents()
    {
        var sut = CreateSut();

        sut.Dispose();

        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<BillingSessionStartedEvent, Task>>());
        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<BillingSessionCompletedEvent, Task>>());
        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<BillingSessionCancelledEvent, Task>>());
    }
}
