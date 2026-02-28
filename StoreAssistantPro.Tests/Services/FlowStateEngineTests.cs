using System.ComponentModel;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

public class FlowStateEngineTests : IDisposable
{
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IFocusLockService _focusLock = Substitute.For<IFocusLockService>();
    private readonly IPredictiveFocusService _predictiveFocus = Substitute.For<IPredictiveFocusService>();
    private readonly IInteractionTracker _interactionTracker = Substitute.For<IInteractionTracker>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private FlowStateEngine? _sut;

    public FlowStateEngineTests()
    {
        _regional.Now.Returns(new DateTime(2025, 6, 15, 10, 0, 0));
        _interactionTracker.CurrentSnapshot.Returns(
            InteractionSnapshot.Idle(new DateTime(2025, 6, 15, 10, 0, 0)));
    }

    public void Dispose() => _sut?.Dispose();

    private FlowStateEngine CreateSut()
    {
        _sut = new FlowStateEngine(
            _appState, _focusLock, _predictiveFocus,
            _interactionTracker, _regional, _eventBus,
            NullLogger<FlowStateEngine>.Instance);
        return _sut;
    }

    private void SetManagementMode()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _appState.CurrentBillingSession.Returns(BillingSessionState.None);
        _focusLock.IsFocusLocked.Returns(false);
        _predictiveFocus.IsUserInputActive.Returns(false);
    }

    private void SetBillingIdle()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.None);
        _focusLock.IsFocusLocked.Returns(false);
        _predictiveFocus.IsUserInputActive.Returns(false);
    }

    private void SetBillingActive()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        _focusLock.IsFocusLocked.Returns(false);
        _predictiveFocus.IsUserInputActive.Returns(false);
    }

    private void SetBillingActiveLockedTyping()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        _focusLock.IsFocusLocked.Returns(true);
        _predictiveFocus.IsUserInputActive.Returns(true);
    }

    private void RaisePropertyChanged(INotifyPropertyChanged source, string propertyName)
    {
        source.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            source, new PropertyChangedEventArgs(propertyName));
    }

    // ═══════════════════════════════════════════════════════════════
    // Initial state — Management mode
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void InitialState_ManagementMode_Calm()
    {
        SetManagementMode();

        var sut = CreateSut();

        Assert.Equal(FlowState.Calm, sut.CurrentState);
        Assert.False(sut.IsInFlow);
    }

    [Fact]
    public void InitialState_ManagementMode_ReasonMentionsManagement()
    {
        SetManagementMode();

        var sut = CreateSut();

        Assert.Contains("Management", sut.TransitionReason);
    }

    [Fact]
    public void InitialState_ManagementMode_HasTransitionTime()
    {
        SetManagementMode();

        var sut = CreateSut();

        Assert.Equal(new DateTime(2025, 6, 15, 10, 0, 0), sut.LastTransitionTime);
    }

    // ═══════════════════════════════════════════════════════════════
    // Initial state — Billing idle (no active session)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void InitialState_BillingNoSession_Calm()
    {
        SetBillingIdle();

        var sut = CreateSut();

        Assert.Equal(FlowState.Calm, sut.CurrentState);
    }

    [Theory]
    [InlineData(BillingSessionState.None)]
    [InlineData(BillingSessionState.Completed)]
    [InlineData(BillingSessionState.Cancelled)]
    public void InitialState_BillingNonActiveSession_Calm(BillingSessionState session)
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(session);
        _focusLock.IsFocusLocked.Returns(false);
        _predictiveFocus.IsUserInputActive.Returns(false);

        var sut = CreateSut();

        Assert.Equal(FlowState.Calm, sut.CurrentState);
    }

    // ═══════════════════════════════════════════════════════════════
    // Initial state — Billing active session
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void InitialState_BillingActiveSession_NoInput_Focused()
    {
        SetBillingActive();

        var sut = CreateSut();

        Assert.Equal(FlowState.Focused, sut.CurrentState);
    }

    [Fact]
    public void InitialState_BillingActiveSessionUnlocked_Typing_Focused()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        _focusLock.IsFocusLocked.Returns(false);
        _predictiveFocus.IsUserInputActive.Returns(true);

        var sut = CreateSut();

        // Typing without focus lock → Focused (not Flow)
        Assert.Equal(FlowState.Focused, sut.CurrentState);
    }

    // ═══════════════════════════════════════════════════════════════
    // Initial state — Flow (billing + locked + typing)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void InitialState_BillingLockedTyping_Flow()
    {
        SetBillingActiveLockedTyping();

        var sut = CreateSut();

        Assert.Equal(FlowState.Flow, sut.CurrentState);
        Assert.True(sut.IsInFlow);
    }

    [Fact]
    public void InitialState_Flow_ReasonMentionsPeakConcentration()
    {
        SetBillingActiveLockedTyping();

        var sut = CreateSut();

        Assert.Contains("peak concentration", sut.TransitionReason, StringComparison.OrdinalIgnoreCase);
    }

    // ═══════════════════════════════════════════════════════════════
    // Transition: Calm → Focused (billing session starts)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CalmToFocused_WhenBillingSessionActivates()
    {
        SetManagementMode();
        var sut = CreateSut();

        // Switch to billing with active session
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);

        sut.Recompute();

        Assert.Equal(FlowState.Focused, sut.CurrentState);
    }

    [Fact]
    public void CalmToFocused_PublishesEvent()
    {
        SetManagementMode();
        var sut = CreateSut();

        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);

        sut.Recompute();

        _eventBus.Received(1).PublishAsync(
            Arg.Is<FlowStateChangedEvent>(e =>
                e.Previous == FlowState.Calm &&
                e.Current == FlowState.Focused));
    }

    // ═══════════════════════════════════════════════════════════════
    // Transition: Focused → Flow (typing starts while locked)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FocusedToFlow_WhenInputActivatesWithFocusLock()
    {
        SetBillingActive();
        var sut = CreateSut();
        Assert.Equal(FlowState.Focused, sut.CurrentState);

        _focusLock.IsFocusLocked.Returns(true);
        _predictiveFocus.IsUserInputActive.Returns(true);

        sut.Recompute();

        Assert.Equal(FlowState.Flow, sut.CurrentState);
    }

    [Fact]
    public void FocusedToFlow_PublishesEvent()
    {
        SetBillingActive();
        var sut = CreateSut();

        _focusLock.IsFocusLocked.Returns(true);
        _predictiveFocus.IsUserInputActive.Returns(true);

        sut.Recompute();

        _eventBus.Received(1).PublishAsync(
            Arg.Is<FlowStateChangedEvent>(e =>
                e.Previous == FlowState.Focused &&
                e.Current == FlowState.Flow));
    }

    // ═══════════════════════════════════════════════════════════════
    // Transition: Flow → Focused (typing stops)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FlowToFocused_WhenInputGoesIdle()
    {
        SetBillingActiveLockedTyping();
        var sut = CreateSut();
        Assert.Equal(FlowState.Flow, sut.CurrentState);

        _predictiveFocus.IsUserInputActive.Returns(false);

        sut.Recompute();

        Assert.Equal(FlowState.Focused, sut.CurrentState);
    }

    // ═══════════════════════════════════════════════════════════════
    // Transition: Focused → Calm (session completes)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FocusedToCalm_WhenSessionCompletes()
    {
        SetBillingActive();
        var sut = CreateSut();

        _appState.CurrentBillingSession.Returns(BillingSessionState.Completed);

        sut.Recompute();

        Assert.Equal(FlowState.Calm, sut.CurrentState);
    }

    [Fact]
    public void FocusedToCalm_WhenSessionCancelled()
    {
        SetBillingActive();
        var sut = CreateSut();

        _appState.CurrentBillingSession.Returns(BillingSessionState.Cancelled);

        sut.Recompute();

        Assert.Equal(FlowState.Calm, sut.CurrentState);
    }

    // ═══════════════════════════════════════════════════════════════
    // Transition: Flow → Calm (mode switches to Management)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FlowToCalm_WhenModeChangesToManagement()
    {
        SetBillingActiveLockedTyping();
        var sut = CreateSut();
        Assert.Equal(FlowState.Flow, sut.CurrentState);

        _appState.CurrentMode.Returns(OperationalMode.Management);

        sut.Recompute();

        Assert.Equal(FlowState.Calm, sut.CurrentState);
    }

    // ═══════════════════════════════════════════════════════════════
    // No-op: same state → no event
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Recompute_SameState_DoesNotPublishEvent()
    {
        SetManagementMode();
        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        sut.Recompute(); // still Management → Calm

        _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<FlowStateChangedEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Event subscription — PropertyChanged triggers
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void SubscribesToAppStatePropertyChanged()
    {
        SetManagementMode();
        var sut = CreateSut();

        _appState.Received().PropertyChanged += Arg.Any<PropertyChangedEventHandler>();
    }

    [Fact]
    public void SubscribesToFocusLockPropertyChanged()
    {
        SetManagementMode();
        var sut = CreateSut();

        _focusLock.Received().PropertyChanged += Arg.Any<PropertyChangedEventHandler>();
    }

    [Fact]
    public void SubscribesToPredictiveFocusPropertyChanged()
    {
        SetManagementMode();
        var sut = CreateSut();

        _predictiveFocus.Received().PropertyChanged += Arg.Any<PropertyChangedEventHandler>();
    }

    // ═══════════════════════════════════════════════════════════════
    // Management mode always overrides to Calm
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ManagementMode_AlwaysCalm_RegardlessOfLockAndInput(
        bool focusLocked, bool inputActive)
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _appState.CurrentBillingSession.Returns(BillingSessionState.None);
        _focusLock.IsFocusLocked.Returns(focusLocked);
        _predictiveFocus.IsUserInputActive.Returns(inputActive);

        var sut = CreateSut();

        Assert.Equal(FlowState.Calm, sut.CurrentState);
    }

    // ═══════════════════════════════════════════════════════════════
    // Flow requires ALL three: billing active + locked + typing
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void BillingActive_LockedNoInput_Focused()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        _focusLock.IsFocusLocked.Returns(true);
        _predictiveFocus.IsUserInputActive.Returns(false);

        var sut = CreateSut();

        Assert.Equal(FlowState.Focused, sut.CurrentState);
    }

    [Fact]
    public void BillingActive_UnlockedTyping_Focused()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        _focusLock.IsFocusLocked.Returns(false);
        _predictiveFocus.IsUserInputActive.Returns(true);

        var sut = CreateSut();

        // Typing without focus lock → Focused, not Flow
        Assert.Equal(FlowState.Focused, sut.CurrentState);
    }

    // ═══════════════════════════════════════════════════════════════
    // Dispose cleanup
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_UnsubscribesFromAllSources()
    {
        SetManagementMode();
        var sut = CreateSut();

        sut.Dispose();

        _appState.Received().PropertyChanged -= Arg.Any<PropertyChangedEventHandler>();
        _focusLock.Received().PropertyChanged -= Arg.Any<PropertyChangedEventHandler>();
        _predictiveFocus.Received().PropertyChanged -= Arg.Any<PropertyChangedEventHandler>();
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        SetManagementMode();
        var sut = CreateSut();

        sut.Dispose();
        sut.Dispose();

        // Only unsubscribes once
        _appState.Received(1).PropertyChanged -= Arg.Any<PropertyChangedEventHandler>();
    }

    // ═══════════════════════════════════════════════════════════════
    // IsInFlow convenience property
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void IsInFlow_TrueOnlyInFlowState()
    {
        SetManagementMode();
        var sut = CreateSut();
        Assert.False(sut.IsInFlow);

        SetBillingActive();
        sut.Recompute();
        Assert.False(sut.IsInFlow);

        _focusLock.IsFocusLocked.Returns(true);
        _predictiveFocus.IsUserInputActive.Returns(true);
        sut.Recompute();
        Assert.True(sut.IsInFlow);
    }

    // ═══════════════════════════════════════════════════════════════
    // FlowState enum
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(FlowState.Calm)]
    [InlineData(FlowState.Focused)]
    [InlineData(FlowState.Flow)]
    public void FlowState_AllValuesAreDefined(FlowState state)
    {
        Assert.True(Enum.IsDefined(state));
    }

    // ═══════════════════════════════════════════════════════════════
    // FlowStateChangedEvent record
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Event_RecordEquality()
    {
        var a = new FlowStateChangedEvent(FlowState.Calm, FlowState.Focused, "session started");
        var b = new FlowStateChangedEvent(FlowState.Calm, FlowState.Focused, "session started");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Event_DifferentStates_NotEqual()
    {
        var a = new FlowStateChangedEvent(FlowState.Calm, FlowState.Focused, "reason");
        var b = new FlowStateChangedEvent(FlowState.Focused, FlowState.Flow, "reason");

        Assert.NotEqual(a, b);
    }

    // ═══════════════════════════════════════════════════════════════
    // TransitionReason updates on state change
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionReason_UpdatesOnEachTransition()
    {
        SetManagementMode();
        var sut = CreateSut();
        var firstReason = sut.TransitionReason;

        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        sut.Recompute();

        Assert.NotEqual(firstReason, sut.TransitionReason);
    }

    // ═══════════════════════════════════════════════════════════════
    // LastTransitionTime updates on state change
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void LastTransitionTime_UpdatesOnTransition()
    {
        SetManagementMode();
        var sut = CreateSut();

        _regional.Now.Returns(new DateTime(2025, 6, 15, 11, 30, 0));
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        sut.Recompute();

        Assert.Equal(new DateTime(2025, 6, 15, 11, 30, 0), sut.LastTransitionTime);
    }

    [Fact]
    public void LastTransitionTime_DoesNotUpdateWithoutStateChange()
    {
        SetManagementMode();
        var sut = CreateSut();
        var initialTime = sut.LastTransitionTime;

        _regional.Now.Returns(new DateTime(2025, 6, 15, 12, 0, 0));
        sut.Recompute(); // still Calm

        Assert.Equal(initialTime, sut.LastTransitionTime);
    }

    // ═══════════════════════════════════════════════════════════════
    // Full lifecycle: Calm → Focused → Flow → Focused → Calm
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FullLifecycle_CalmFocusedFlowFocusedCalm()
    {
        SetManagementMode();
        var sut = CreateSut();
        Assert.Equal(FlowState.Calm, sut.CurrentState);

        // Step 1: Calm → Focused
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        sut.Recompute();
        Assert.Equal(FlowState.Focused, sut.CurrentState);

        // Step 2: Focused → Flow
        _focusLock.IsFocusLocked.Returns(true);
        _predictiveFocus.IsUserInputActive.Returns(true);
        sut.Recompute();
        Assert.Equal(FlowState.Flow, sut.CurrentState);

        // Step 3: Flow → Focused (typing stops)
        _predictiveFocus.IsUserInputActive.Returns(false);
        sut.Recompute();
        Assert.Equal(FlowState.Focused, sut.CurrentState);

        // Step 4: Focused → Calm (session completes)
        _appState.CurrentBillingSession.Returns(BillingSessionState.Completed);
        _focusLock.IsFocusLocked.Returns(false);
        sut.Recompute();
        Assert.Equal(FlowState.Calm, sut.CurrentState);
    }

    [Fact]
    public void FullLifecycle_PublishesEventForEachTransition()
    {
        SetManagementMode();
        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        // Calm → Focused
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.CurrentBillingSession.Returns(BillingSessionState.Active);
        sut.Recompute();

        // Focused → Flow
        _focusLock.IsFocusLocked.Returns(true);
        _predictiveFocus.IsUserInputActive.Returns(true);
        sut.Recompute();

        // Flow → Focused
        _predictiveFocus.IsUserInputActive.Returns(false);
        sut.Recompute();

        // Focused → Calm
        _appState.CurrentBillingSession.Returns(BillingSessionState.Completed);
        _focusLock.IsFocusLocked.Returns(false);
        sut.Recompute();

        // 4 transitions = 4 events
        _eventBus.Received(4).PublishAsync(
            Arg.Any<FlowStateChangedEvent>());
    }
}
