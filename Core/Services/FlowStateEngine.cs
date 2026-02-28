using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton implementation of <see cref="IFlowStateEngine"/>.
/// <para>
/// Subscribes to <see cref="IAppStateService.PropertyChanged"/>,
/// <see cref="IFocusLockService.PropertyChanged"/>, and
/// <see cref="IPredictiveFocusService.PropertyChanged"/> to
/// recompute the <see cref="FlowState"/> on every relevant change.
/// </para>
///
/// <para><b>Decision matrix:</b></para>
/// <code>
///   ┌────────────────────────┬──────────┬───────┬──────────────────────────┐
///   │ Condition              │ Mode     │ Lock  │ Flow Determination       │
///   ├────────────────────────┼──────────┼───────┼──────────────────────────┤
///   │ Calm                   │ Mgmt     │ any   │ —                        │
///   │ Calm                   │ Billing  │ any   │ —                        │
///   │  (session != Active)   │          │       │                          │
///   │ Flow                   │ Billing  │ true  │ Analyzer score ≥ 0.6     │
///   │  (session == Active)   │          │       │  OR legacy input active  │
///   │ Focused                │ Billing  │ true  │ Analyzer score &lt; 0.6     │
///   │  (session == Active)   │          │       │  AND no legacy input     │
///   │ Focused                │ Billing  │ false │ —                        │
///   │  (session == Active)   │          │       │                          │
///   └────────────────────────┴──────────┴───────┴──────────────────────────┘
///
///   The FlowStateAnalyzer uses interaction metrics (typing speed,
///   idle time, billing action rate, mouse activity) to produce a
///   weighted score. Focus lock must be engaged for Flow state.
/// </code>
/// </summary>
public sealed partial class FlowStateEngine : ObservableObject, IFlowStateEngine
{
    private readonly IAppStateService _appState;
    private readonly IFocusLockService _focusLock;
    private readonly IPredictiveFocusService _predictiveFocus;
    private readonly IInteractionTracker _interactionTracker;
    private readonly IRegionalSettingsService _regional;
    private readonly IEventBus _eventBus;
    private readonly ILogger<FlowStateEngine> _logger;
    private readonly Lock _lock = new();
    private bool _disposed;

    public FlowStateEngine(
        IAppStateService appState,
        IFocusLockService focusLock,
        IPredictiveFocusService predictiveFocus,
        IInteractionTracker interactionTracker,
        IRegionalSettingsService regional,
        IEventBus eventBus,
        ILogger<FlowStateEngine> logger)
    {
        _appState = appState;
        _focusLock = focusLock;
        _predictiveFocus = predictiveFocus;
        _interactionTracker = interactionTracker;
        _regional = regional;
        _eventBus = eventBus;
        _logger = logger;

        // Subscribe to signal sources
        _appState.PropertyChanged += OnSignalChanged;
        _focusLock.PropertyChanged += OnSignalChanged;
        _predictiveFocus.PropertyChanged += OnSignalChanged;
        _interactionTracker.PropertyChanged += OnSignalChanged;

        // Compute initial state — set properties directly (no event yet)
        var (state, reason) = ComputeState();
        CurrentState = state;
        TransitionReason = reason;
        LastTransitionTime = _regional.Now;

        _logger.LogDebug(
            "FlowStateEngine initialized: {State} — {Reason}",
            state, reason);
    }

    // ── Observable properties ────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInFlow))]
    public partial FlowState CurrentState { get; private set; }

    [ObservableProperty]
    public partial string TransitionReason { get; private set; }

    [ObservableProperty]
    public partial DateTime LastTransitionTime { get; private set; }

    public bool IsInFlow => CurrentState == FlowState.Flow;

    // ── Recomputation ────────────────────────────────────────────────

    public void Recompute()
    {
        FlowState previousState;
        FlowState newState;
        string reason;

        lock (_lock)
        {
            previousState = CurrentState;
            (newState, reason) = ComputeState();

            if (newState == previousState)
                return;

            CurrentState = newState;
            TransitionReason = reason;
            LastTransitionTime = _regional.Now;
        }

        _logger.LogInformation(
            "FlowState: {Previous} → {Current} — {Reason}",
            previousState, newState, reason);

        _ = _eventBus.PublishAsync(new FlowStateChangedEvent(
            previousState, newState, reason));
    }

    // ── Decision engine ──────────────────────────────────────────────

    private (FlowState State, string Reason) ComputeState()
    {
        // ── Rule 1: Management mode → always Calm ────────────────
        if (_appState.CurrentMode == OperationalMode.Management)
            return (FlowState.Calm, "Management mode — full chrome, low urgency.");

        // ── Rule 2: Billing but no active session → Calm ─────────
        if (_appState.CurrentBillingSession != BillingSessionState.Active)
        {
            var sessionState = _appState.CurrentBillingSession;
            return (FlowState.Calm,
                $"Billing mode, session {sessionState} — between customers.");
        }

        // ── At this point: Billing mode + Active session ─────────

        // ── Rule 3: Focus locked → use FlowStateAnalyzer to
        //    determine Flow vs Focused from interaction metrics ────
        if (_focusLock.IsFocusLocked)
        {
            var snapshot = _interactionTracker.CurrentSnapshot;
            var analysis = FlowStateAnalyzer.Analyze(snapshot);

            if (analysis.RecommendedState == FlowState.Flow)
                return (FlowState.Flow,
                    $"Billing active, focus locked, analyzer: {analysis.Summary}");

            // Analyzer says not quite Flow — check legacy input signal as fallback
            if (_predictiveFocus.IsUserInputActive)
                return (FlowState.Flow,
                    "Billing session active, focus locked, input detected — peak concentration.");

            return (FlowState.Focused,
                $"Billing active, focus locked, analyzer: {analysis.Summary}");
        }

        // ── Rule 4: Active session, focus unlocked → Focused ─────
        var inputLabel = _predictiveFocus.IsUserInputActive ? "typing" : "idle";
        return (FlowState.Focused,
            $"Billing session active, focus unlocked, input {inputLabel} — moderate concentration.");
    }

    // ── Event handler ────────────────────────────────────────────────

    private void OnSignalChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Only recompute on state changes that affect the flow decision
        switch (e.PropertyName)
        {
            case nameof(IAppStateService.CurrentMode):
            case nameof(IAppStateService.CurrentBillingSession):
            case nameof(IFocusLockService.IsFocusLocked):
            case nameof(IPredictiveFocusService.IsUserInputActive):
            case nameof(IInteractionTracker.CurrentSnapshot):
                Recompute();
                break;
        }
    }

    // ── Cleanup ──────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _appState.PropertyChanged -= OnSignalChanged;
        _focusLock.PropertyChanged -= OnSignalChanged;
        _predictiveFocus.PropertyChanged -= OnSignalChanged;
        _interactionTracker.PropertyChanged -= OnSignalChanged;
    }
}
