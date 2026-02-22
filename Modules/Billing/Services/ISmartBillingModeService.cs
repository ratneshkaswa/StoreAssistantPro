namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Bridges <see cref="IBillingSessionService"/> lifecycle events to
/// <see cref="IBillingModeService"/> operational mode transitions.
/// <para>
/// <b>Auto mode switching rules:</b>
/// </para>
/// <list type="bullet">
///   <item>Session → <c>Active</c>: switches to <see cref="Models.OperationalMode.Billing"/>.</item>
///   <item>Session → <c>Completed</c> or <c>Cancelled</c>: switches back to <see cref="Models.OperationalMode.Management"/>.</item>
/// </list>
/// <para>
/// <b>Safety rules:</b>
/// </para>
/// <list type="bullet">
///   <item>Mode cannot exit Billing while a session is still <c>Active</c>.</item>
///   <item>All transitions are blocked while payment is processing
///         (see <see cref="BeginPaymentProcessing"/> / <see cref="EndPaymentProcessing"/>).</item>
///   <item>Concurrent events are serialized to prevent race conditions.</item>
/// </list>
/// <para>
/// Loop avoidance is guaranteed by <see cref="IBillingModeService"/>
/// which treats same-mode transitions as no-ops.
/// </para>
/// <para>
/// Registered as a <b>singleton</b>. Call <see cref="IDisposable.Dispose"/>
/// (or let the DI container dispose) to unsubscribe from the event bus.
/// </para>
/// </summary>
public interface ISmartBillingModeService : IDisposable
{
    /// <summary>
    /// <c>true</c> while a payment is being processed. All mode
    /// transitions are blocked until <see cref="EndPaymentProcessing"/>
    /// is called.
    /// </summary>
    bool IsPaymentProcessing { get; }

    /// <summary>
    /// Signals that a payment operation has started. Mode transitions
    /// are blocked until <see cref="EndPaymentProcessing"/> is called.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if already processing a payment.
    /// </exception>
    void BeginPaymentProcessing();

    /// <summary>
    /// Signals that the payment operation has finished (success or
    /// failure). If a mode transition was deferred, it executes now.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no payment is currently processing.
    /// </exception>
    Task EndPaymentProcessingAsync();
}
