using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

public class BillingModeServiceTests
{
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IFeatureToggleService _featureToggle = Substitute.For<IFeatureToggleService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private BillingModeService CreateSut() => new(_appState, _featureToggle, _eventBus);

    // ── StartBilling ───────────────────────────────────────────────

    [Fact]
    public async Task StartBilling_FromManagement_SetsModeToBilling()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        var sut = CreateSut();

        await sut.StartBillingAsync();

        _appState.Received(1).SetMode(OperationalMode.Billing);
    }

    [Fact]
    public async Task StartBilling_FromManagement_PublishesEvent()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        var sut = CreateSut();

        await sut.StartBillingAsync();

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<OperationalModeChangedEvent>(e =>
                e.PreviousMode == OperationalMode.Management &&
                e.NewMode == OperationalMode.Billing));
    }

    [Fact]
    public async Task StartBilling_AlreadyBilling_IsNoOp()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        var sut = CreateSut();

        await sut.StartBillingAsync();

        _appState.DidNotReceive().SetMode(Arg.Any<OperationalMode>());
        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<OperationalModeChangedEvent>());
    }

    [Fact]
    public async Task StartBilling_ClearsDeferredStop()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        var sut = CreateSut();

        // Queue a deferred stop
        await sut.StopBillingAsync();
        Assert.True(sut.IsStopDeferred);

        // Start billing clears it
        await sut.StartBillingAsync();
        Assert.False(sut.IsStopDeferred);
    }

    // ── StopBilling ────────────────────────────────────────────────

    [Fact]
    public async Task StopBilling_FromBilling_SetsModeToManagement()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.None);
        var sut = CreateSut();

        await sut.StopBillingAsync();

        _appState.Received(1).SetMode(OperationalMode.Management);
    }

    [Fact]
    public async Task StopBilling_FromBilling_PublishesEvent()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.None);
        var sut = CreateSut();

        await sut.StopBillingAsync();

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<OperationalModeChangedEvent>(e =>
                e.PreviousMode == OperationalMode.Billing &&
                e.NewMode == OperationalMode.Management));
    }

    [Fact]
    public async Task StopBilling_AlreadyManagement_IsNoOp()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _appState.CurrentBillingSession.Returns(BillingSessionState.None);
        var sut = CreateSut();

        await sut.StopBillingAsync();

        _appState.DidNotReceive().SetMode(Arg.Any<OperationalMode>());
        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<OperationalModeChangedEvent>());
    }

    // ── Deferred stop (session is Active) ──────────────────────────

    [Fact]
    public async Task StopBilling_WhileSessionActive_DefersSwitch()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        var sut = CreateSut();

        await sut.StopBillingAsync();

        Assert.True(sut.IsStopDeferred);
        _appState.DidNotReceive().SetMode(Arg.Any<OperationalMode>());
        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<OperationalModeChangedEvent>());
    }

    [Fact]
    public async Task StopBilling_SessionCompleted_ExecutesImmediately()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Completed);
        var sut = CreateSut();

        await sut.StopBillingAsync();

        Assert.False(sut.IsStopDeferred);
        _appState.Received(1).SetMode(OperationalMode.Management);
    }

    [Fact]
    public async Task StopBilling_SessionCancelled_ExecutesImmediately()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Cancelled);
        var sut = CreateSut();

        await sut.StopBillingAsync();

        Assert.False(sut.IsStopDeferred);
        _appState.Received(1).SetMode(OperationalMode.Management);
    }

    // ── FlushDeferredStop ──────────────────────────────────────────

    [Fact]
    public async Task FlushDeferredStop_WhenDeferred_SwitchesToManagement()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        var sut = CreateSut();

        await sut.StopBillingAsync();  // deferred
        Assert.True(sut.IsStopDeferred);

        await sut.FlushDeferredStopAsync();

        Assert.False(sut.IsStopDeferred);
        _appState.Received(1).SetMode(OperationalMode.Management);
    }

    [Fact]
    public async Task FlushDeferredStop_WhenNotDeferred_IsNoOp()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        var sut = CreateSut();

        await sut.FlushDeferredStopAsync();

        Assert.False(sut.IsStopDeferred);
        _appState.DidNotReceive().SetMode(Arg.Any<OperationalMode>());
        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<OperationalModeChangedEvent>());
    }

    // ── CurrentMode delegation ─────────────────────────────────────

    [Fact]
    public void CurrentMode_DelegatesToAppState()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        var sut = CreateSut();

        Assert.Equal(OperationalMode.Billing, sut.CurrentMode);
    }
}
