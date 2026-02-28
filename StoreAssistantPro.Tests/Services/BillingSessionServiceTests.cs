using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

public class BillingSessionServiceTests
{
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private BillingSessionService CreateSut() => new(_appState, _eventBus);

    // ── Initial state ──────────────────────────────────────────────

    [Fact]
    public void InitialState_IsNone()
    {
        var sut = CreateSut();

        Assert.Equal(BillingSessionState.None, sut.CurrentState);
    }

    // ── StartSession ───────────────────────────────────────────────

    [Fact]
    public async Task StartSession_FromNone_TransitionsToActive()
    {
        var sut = CreateSut();

        await sut.StartSessionAsync();

        Assert.Equal(BillingSessionState.Active, sut.CurrentState);
    }

    [Fact]
    public async Task StartSession_FromNone_UpdatesAppState()
    {
        var sut = CreateSut();

        await sut.StartSessionAsync();

        _appState.Received(1).SetBillingSession(BillingSessionState.Active);
    }

    [Fact]
    public async Task StartSession_FromNone_PublishesEvent()
    {
        var sut = CreateSut();

        await sut.StartSessionAsync();

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<BillingSessionStateChangedEvent>(e =>
                e.PreviousState == BillingSessionState.None &&
                e.NewState == BillingSessionState.Active));
    }

    [Fact]
    public async Task StartSession_FromNone_PublishesStartedEvent()
    {
        var sut = CreateSut();

        await sut.StartSessionAsync();

        await _eventBus.Received(1).PublishAsync(
            Arg.Any<BillingSessionStartedEvent>());
    }

    [Fact]
    public async Task StartSession_FromCompleted_TransitionsToActive()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();
        await sut.CompleteSessionAsync();

        await sut.StartSessionAsync();

        Assert.Equal(BillingSessionState.Active, sut.CurrentState);
    }

    [Fact]
    public async Task StartSession_FromCancelled_TransitionsToActive()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();
        await sut.CancelSessionAsync();

        await sut.StartSessionAsync();

        Assert.Equal(BillingSessionState.Active, sut.CurrentState);
    }

    [Fact]
    public async Task StartSession_FromActive_Throws()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.StartSessionAsync());
    }

    // ── CompleteSession ────────────────────────────────────────────

    [Fact]
    public async Task CompleteSession_FromActive_TransitionsToCompleted()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();

        await sut.CompleteSessionAsync();

        Assert.Equal(BillingSessionState.Completed, sut.CurrentState);
    }

    [Fact]
    public async Task CompleteSession_FromActive_UpdatesAppState()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();
        _appState.ClearReceivedCalls();

        await sut.CompleteSessionAsync();

        _appState.Received(1).SetBillingSession(BillingSessionState.Completed);
    }

    [Fact]
    public async Task CompleteSession_FromActive_PublishesEvent()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();
        _eventBus.ClearReceivedCalls();

        await sut.CompleteSessionAsync();

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<BillingSessionStateChangedEvent>(e =>
                e.PreviousState == BillingSessionState.Active &&
                e.NewState == BillingSessionState.Completed));
    }

    [Fact]
    public async Task CompleteSession_FromActive_PublishesCompletedEvent()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();
        _eventBus.ClearReceivedCalls();

        await sut.CompleteSessionAsync();

        await _eventBus.Received(1).PublishAsync(
            Arg.Any<BillingSessionCompletedEvent>());
    }

    [Fact]
    public async Task CompleteSession_FromNone_Throws()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CompleteSessionAsync());
    }

    [Fact]
    public async Task CompleteSession_FromCompleted_Throws()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();
        await sut.CompleteSessionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CompleteSessionAsync());
    }

    // ── CancelSession ──────────────────────────────────────────────

    [Fact]
    public async Task CancelSession_FromActive_TransitionsToCancelled()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();

        await sut.CancelSessionAsync();

        Assert.Equal(BillingSessionState.Cancelled, sut.CurrentState);
    }

    [Fact]
    public async Task CancelSession_FromActive_UpdatesAppState()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();
        _appState.ClearReceivedCalls();

        await sut.CancelSessionAsync();

        _appState.Received(1).SetBillingSession(BillingSessionState.Cancelled);
    }

    [Fact]
    public async Task CancelSession_FromActive_PublishesEvent()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();
        _eventBus.ClearReceivedCalls();

        await sut.CancelSessionAsync();

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<BillingSessionStateChangedEvent>(e =>
                e.PreviousState == BillingSessionState.Active &&
                e.NewState == BillingSessionState.Cancelled));
    }

    [Fact]
    public async Task CancelSession_FromActive_PublishesCancelledEvent()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();
        _eventBus.ClearReceivedCalls();

        await sut.CancelSessionAsync();

        await _eventBus.Received(1).PublishAsync(
            Arg.Any<BillingSessionCancelledEvent>());
    }

    [Fact]
    public async Task CancelSession_FromNone_Throws()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CancelSessionAsync());
    }

    [Fact]
    public async Task CancelSession_FromCancelled_Throws()
    {
        var sut = CreateSut();
        await sut.StartSessionAsync();
        await sut.CancelSessionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CancelSessionAsync());
    }

    // ── Full lifecycle ─────────────────────────────────────────────

    [Fact]
    public async Task FullLifecycle_Start_Complete_Start_Cancel()
    {
        var sut = CreateSut();

        await sut.StartSessionAsync();
        Assert.Equal(BillingSessionState.Active, sut.CurrentState);

        await sut.CompleteSessionAsync();
        Assert.Equal(BillingSessionState.Completed, sut.CurrentState);

        await sut.StartSessionAsync();
        Assert.Equal(BillingSessionState.Active, sut.CurrentState);

        await sut.CancelSessionAsync();
        Assert.Equal(BillingSessionState.Cancelled, sut.CurrentState);
    }
}
