using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Manages the lifecycle of a single billing (POS) session.
/// <para>
/// Tracks the current <see cref="BillingSessionState"/> and publishes
/// <see cref="Events.BillingSessionStateChangedEvent"/> via
/// <see cref="Core.Events.IEventBus"/> on every transition.
/// </para>
/// <para>
/// <b>State machine:</b>
/// </para>
/// <code>
///   None ──▶ Active ──▶ Completed ──▶ Active | None
///                  └──▶ Cancelled ──▶ Active | None
/// </code>
/// <para>
/// Registered as a <b>singleton</b>. Contains no UI logic —
/// ViewModels subscribe to events or read <see cref="CurrentState"/>.
/// </para>
/// </summary>
public interface IBillingSessionService
{
    /// <summary>Current session lifecycle state.</summary>
    BillingSessionState CurrentState { get; }

    /// <summary>
    /// Starts a new billing session.
    /// Valid from <see cref="BillingSessionState.None"/>,
    /// <see cref="BillingSessionState.Completed"/>, or
    /// <see cref="BillingSessionState.Cancelled"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called from <see cref="BillingSessionState.Active"/>.
    /// </exception>
    Task StartSessionAsync();

    /// <summary>
    /// Marks the current session as successfully completed.
    /// Valid only from <see cref="BillingSessionState.Active"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there is no active session.
    /// </exception>
    Task CompleteSessionAsync();

    /// <summary>
    /// Cancels the current session, discarding the cart.
    /// Valid only from <see cref="BillingSessionState.Active"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there is no active session.
    /// </exception>
    Task CancelSessionAsync();
}
