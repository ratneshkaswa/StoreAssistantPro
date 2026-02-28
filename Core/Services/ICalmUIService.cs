using System.ComponentModel;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Semantic layer that decides visual emphasis across the shell.
/// <para>
/// <b>Calm UI</b> is a design principle that reduces visual noise by
/// de-emphasising chrome (menu, toolbar, status bar) when the user is
/// focused on content — especially during billing sessions. The service
/// reacts to mode changes, focus lock, billing session state, and
/// connectivity to compute an <see cref="EmphasisLevel"/> for each
/// <see cref="WorkspaceZone"/>.
/// </para>
/// <para>
/// XAML-side behaviors (<c>BillingDimBehavior</c>, <c>AdaptiveWorkspace</c>)
/// are the visual executors; this service is the <b>decision engine</b>
/// that tells them what emphasis to apply.
/// </para>
///
/// <para><b>Calm mode rules:</b></para>
/// <list type="number">
///   <item>When <see cref="IFocusLockService.IsFocusLocked"/> is <c>true</c>
///         (billing focus), calm mode activates automatically — chrome
///         zones recede and the content zone gets full emphasis.</item>
///   <item>When the <see cref="IAppStateService.CurrentMode"/> is
///         <c>Billing</c> but focus is not locked (between customers),
///         calm mode applies a milder <see cref="EmphasisLevel.Muted"/>
///         to chrome to maintain billing context without full lock-down.</item>
///   <item>In <c>Management</c> mode, calm mode is off — all zones get
///         <see cref="EmphasisLevel.Full"/>.</item>
///   <item><see cref="CalmModeEnabled"/> can be overridden manually
///         for testing or accessibility.</item>
/// </list>
///
/// <para><b>Architecture rules:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b>.</item>
///   <item>Listens to <see cref="IAppStateService.PropertyChanged"/>
///         and <see cref="IFocusLockService.PropertyChanged"/> — no
///         direct coupling to any ViewModel.</item>
///   <item>Publishes <see cref="Events.CalmStateChangedEvent"/> on
///         every recomputation so subscribers update reactively.</item>
///   <item>ViewModels read <see cref="GetEmphasis"/> or bind to
///         <see cref="ActiveZone"/>/<see cref="CalmModeEnabled"/>.</item>
/// </list>
/// </summary>
public interface ICalmUIService : INotifyPropertyChanged
{
    /// <summary>
    /// The zone that currently has primary user attention.
    /// <see cref="WorkspaceZone.Content"/> during billing;
    /// <see cref="WorkspaceZone.Content"/> in management (balanced).
    /// </summary>
    WorkspaceZone ActiveZone { get; }

    /// <summary>
    /// <c>true</c> when the calm system is actively reducing visual
    /// noise. Driven automatically by mode + focus lock state, but
    /// can be overridden via <see cref="SetCalmModeEnabled"/>.
    /// </summary>
    bool CalmModeEnabled { get; }

    /// <summary>
    /// <c>true</c> when no manual override is active (the system decides
    /// calm state automatically). <c>false</c> when the user has
    /// explicitly disabled calm mode via <see cref="SetCalmModeEnabled"/>.
    /// Used by the settings UI to reflect the toggle state.
    /// </summary>
    bool IsCalmAutomatic { get; }

    /// <summary>
    /// The operator's current cognitive engagement level, sourced from
    /// <see cref="IFlowStateEngine.CurrentState"/>. Drives additional
    /// visual noise reduction beyond normal calm emphasis.
    /// <para>
    /// When <see cref="FlowState.Flow"/>, <see cref="GetEmphasis"/>
    /// intensifies chrome recession (Muted → Receded) for maximum
    /// focus on the active zone.
    /// </para>
    /// </summary>
    FlowState CurrentFlowState { get; }

    /// <summary>
    /// Returns the current emphasis level for a given zone.
    /// </summary>
    EmphasisLevel GetEmphasis(WorkspaceZone zone);

    /// <summary>
    /// Manual override for calm mode (e.g., from accessibility settings).
    /// Pass <c>null</c> to return to automatic behaviour.
    /// </summary>
    void SetCalmModeEnabled(bool? enabled);
}
