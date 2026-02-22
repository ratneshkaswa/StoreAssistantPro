namespace StoreAssistantPro.Models;

/// <summary>
/// Tracks the lifecycle of a single billing (POS) session.
/// <para>
/// Used by <see cref="Core.Services.IAppStateService"/> and
/// <see cref="Modules.Billing.Services.IBillingModeService"/> to
/// coordinate auto mode switching:
/// </para>
/// <list type="bullet">
///   <item><see cref="None"/> → no session exists; app is in Management mode.</item>
///   <item><see cref="Active"/> → operator is building a cart / processing payment.</item>
///   <item><see cref="Completed"/> → sale finalized and receipt generated; ready for next customer or mode switch.</item>
///   <item><see cref="Cancelled"/> → session was abandoned; ready for next customer or mode switch.</item>
/// </list>
/// <para>
/// Transitions follow this state machine:
/// </para>
/// <code>
///   None ──▶ Active ──▶ Completed
///                  └──▶ Cancelled
///   Completed ──▶ Active  (next customer)
///   Cancelled ──▶ Active  (next customer)
///   Completed ──▶ None    (stop billing)
///   Cancelled ──▶ None    (stop billing)
/// </code>
/// </summary>
public enum BillingSessionState
{
    /// <summary>
    /// No billing session exists. The application is in Management mode
    /// or billing has not yet started.
    /// </summary>
    None = 0,

    /// <summary>
    /// A billing session is in progress — the operator is adding items,
    /// applying discounts, or processing payment.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The billing session finished successfully — the sale was saved
    /// and a receipt was generated.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The billing session was cancelled — the cart was discarded
    /// without completing a sale.
    /// </summary>
    Cancelled = 3
}
