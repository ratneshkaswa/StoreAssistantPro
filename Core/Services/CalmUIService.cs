using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton decision engine that computes visual emphasis for each
/// <see cref="WorkspaceZone"/> based on the current application state.
/// <para>
/// Reacts to <see cref="IAppStateService.PropertyChanged"/>,
/// <see cref="IFocusLockService.PropertyChanged"/>, and
/// <see cref="IFlowStateEngine.PropertyChanged"/> to recompute
/// emphasis automatically. Publishes <see cref="CalmStateChangedEvent"/>
/// after every recomputation.
/// </para>
///
/// <para><b>Emphasis matrix (with flow-state integration):</b></para>
/// <code>
///   ┌──────────────────────────┬───────────┬──────────┬──────────┬───────────┐
///   │ Scenario                 │ MenuBar   │ Toolbar  │ Content  │ StatusBar │
///   ├──────────────────────────┼───────────┼──────────┼──────────┼───────────┤
///   │ Management (Calm)        │ Full      │ Full     │ Full     │ Full      │
///   │ Billing unlocked (Calm)  │ Muted     │ Muted    │ Full     │ Muted     │
///   │ Billing unlocked (Focus) │ Muted     │ Muted    │ Full     │ Muted     │
///   │ Billing locked (Focused) │ Receded   │ Receded  │ Full     │ Receded   │
///   │ Billing locked (Flow)    │ Receded   │ Receded  │ Full     │ Receded   │
///   │ Billing unlocked (Flow)  │ Receded   │ Receded  │ Full     │ Receded   │
///   └──────────────────────────┴───────────┴──────────┴──────────┴───────────┘
///
///   Key change: Flow state intensifies Muted → Receded on chrome zones,
///   providing maximum noise reduction during peak concentration.
/// </code>
/// </summary>
public sealed partial class CalmUIService : ObservableObject, ICalmUIService, IDisposable
{
    private readonly IAppStateService _appState;
    private readonly IFocusLockService _focusLock;
    private readonly IFlowStateEngine _flowStateEngine;
    private readonly IEventBus _eventBus;
    private bool? _manualOverride;
    private bool _disposed;

    public CalmUIService(
        IAppStateService appState,
        IFocusLockService focusLock,
        IFlowStateEngine flowStateEngine,
        IEventBus eventBus)
    {
        _appState = appState;
        _focusLock = focusLock;
        _flowStateEngine = flowStateEngine;
        _eventBus = eventBus;

        _appState.PropertyChanged += OnSourceStateChanged;
        _focusLock.PropertyChanged += OnSourceStateChanged;
        _flowStateEngine.PropertyChanged += OnFlowStateChanged;

        // Compute initial state (also sets CurrentFlowState)
        Recompute();
    }

    // ── Observable properties ────────────────────────────────────────

    [ObservableProperty]
    public partial WorkspaceZone ActiveZone { get; private set; }

    [ObservableProperty]
    public partial bool CalmModeEnabled { get; private set; }

    [ObservableProperty]
    public partial FlowState CurrentFlowState { get; private set; }

    /// <inheritdoc/>
    public bool IsCalmAutomatic => !_manualOverride.HasValue;

    // ── Emphasis query ───────────────────────────────────────────────

    public EmphasisLevel GetEmphasis(WorkspaceZone zone)
    {
        // The active zone always gets full emphasis
        if (zone == ActiveZone)
            return EmphasisLevel.Full;

        if (!CalmModeEnabled)
            return EmphasisLevel.Full;

        // Focus-locked → chrome recedes hard
        if (_focusLock.IsFocusLocked)
            return EmphasisLevel.Receded;

        // Flow state → intensify: Muted becomes Receded for extra noise reduction
        if (CurrentFlowState == FlowState.Flow)
            return EmphasisLevel.Receded;

        // Calm enabled (billing between customers, or manual override)
        // → mild muting of non-active zones
        return EmphasisLevel.Muted;
    }

    // ── Manual override ──────────────────────────────────────────────

    public void SetCalmModeEnabled(bool? enabled)
    {
        _manualOverride = enabled;
        Recompute();
    }

    // ── Recomputation engine ─────────────────────────────────────────

    private void OnSourceStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Only recompute on state changes that affect emphasis
        switch (e.PropertyName)
        {
            case nameof(IAppStateService.CurrentMode):
            case nameof(IAppStateService.CurrentBillingSession):
            case nameof(IAppStateService.IsOfflineMode):
            case nameof(IFocusLockService.IsFocusLocked):
            case nameof(IFocusLockService.ActiveModule):
                Recompute();
                break;
        }
    }

    private void OnFlowStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IFlowStateEngine.CurrentState))
            Recompute();
    }

    private void Recompute()
    {
        var previousZone = ActiveZone;
        var previousCalm = CalmModeEnabled;
        var previousFlow = CurrentFlowState;

        // ── Active zone ──────────────────────────────────────────
        // Content is always the primary zone. In a future multi-panel
        // layout, this could shift to a sidebar or secondary panel.
        ActiveZone = WorkspaceZone.Content;

        // ── Flow state ───────────────────────────────────────────
        CurrentFlowState = _flowStateEngine.CurrentState;

        // ── Calm mode ────────────────────────────────────────────
        if (_manualOverride.HasValue)
        {
            CalmModeEnabled = _manualOverride.Value;
        }
        else
        {
            // Auto: calm activates in billing mode or when focus-locked
            CalmModeEnabled = _focusLock.IsFocusLocked
                           || _appState.CurrentMode == OperationalMode.Billing;
        }

        // ── Publish if anything changed ──────────────────────────
        if (ActiveZone != previousZone || CalmModeEnabled != previousCalm
            || CurrentFlowState != previousFlow)
        {
            _ = _eventBus.PublishAsync(new CalmStateChangedEvent(
                ActiveZone, CalmModeEnabled, CurrentFlowState));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _appState.PropertyChanged -= OnSourceStateChanged;
        _focusLock.PropertyChanged -= OnSourceStateChanged;
        _flowStateEngine.PropertyChanged -= OnFlowStateChanged;
        GC.SuppressFinalize(this);
    }
}
