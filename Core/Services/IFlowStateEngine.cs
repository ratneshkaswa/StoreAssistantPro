using System.ComponentModel;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton engine that computes the operator's current
/// <see cref="FlowState"/> from application signals and publishes
/// <see cref="Events.FlowStateChangedEvent"/> on every transition.
///
/// <para><b>Input signals:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Signal source</term>
///     <description>What it drives</description>
///   </listheader>
///   <item>
///     <term><see cref="IAppStateService.CurrentMode"/></term>
///     <description>Management → always <see cref="FlowState.Calm"/>.</description>
///   </item>
///   <item>
///     <term><see cref="IAppStateService.CurrentBillingSession"/></term>
///     <description>Active → at least <see cref="FlowState.Focused"/>;
///     None/Completed/Cancelled → <see cref="FlowState.Calm"/>.</description>
///   </item>
///   <item>
///     <term><see cref="IFocusLockService.IsFocusLocked"/></term>
///     <description>Locked + active input → <see cref="FlowState.Flow"/>.</description>
///   </item>
///   <item>
///     <term><see cref="IPredictiveFocusService.IsUserInputActive"/></term>
///     <description>Active typing during billing → <see cref="FlowState.Flow"/>;
///     idle → fall back to <see cref="FlowState.Focused"/>.</description>
///   </item>
/// </list>
///
/// <para><b>Relationship to Calm UI:</b></para>
/// <para>
/// The <see cref="ICalmUIService"/> controls <i>visual emphasis</i> per
/// <see cref="WorkspaceZone"/>. The FlowStateEngine controls the
/// <i>cognitive engagement level</i> — a higher-order concept that
/// influences animation intensity, notification priority, and zero-click
/// aggressiveness. Both are singletons and can be queried independently.
/// </para>
///
/// <para><b>Architecture:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b>.</item>
///   <item>Listens to <see cref="IAppStateService.PropertyChanged"/>,
///         <see cref="IFocusLockService.PropertyChanged"/>, and
///         <see cref="IPredictiveFocusService.PropertyChanged"/>.</item>
///   <item>Publishes <see cref="Events.FlowStateChangedEvent"/> on
///         every state change (not on every recompute).</item>
///   <item>No WPF dependency — pure service logic.</item>
///   <item>Thread-safe recomputation via <c>lock</c>.</item>
/// </list>
/// </summary>
public interface IFlowStateEngine : INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// The operator's current cognitive engagement level.
    /// </summary>
    FlowState CurrentState { get; }

    /// <summary>
    /// Human-readable reason for the most recent state transition.
    /// Useful for diagnostics and logging.
    /// </summary>
    string TransitionReason { get; }

    /// <summary>
    /// Timestamp (IST) of the most recent state transition.
    /// </summary>
    DateTime LastTransitionTime { get; }

    /// <summary>
    /// Returns <c>true</c> when the operator is in
    /// <see cref="FlowState.Flow"/> — the highest engagement level.
    /// Convenience shortcut for binding and conditional logic.
    /// </summary>
    bool IsInFlow { get; }

    /// <summary>
    /// Forces recomputation of the current state from all input signals.
    /// Normally called automatically by event handlers, but exposed
    /// for testing and manual refresh.
    /// </summary>
    void Recompute();
}
