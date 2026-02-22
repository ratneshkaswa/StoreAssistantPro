namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Coordinates automatic persistence of the billing cart during an
/// active session so the operator can resume after a crash or restart.
/// <para>
/// <b>Auto-save triggers:</b>
/// </para>
/// <list type="bullet">
///   <item><b>Cart changed</b> (product added, quantity changed, item
///         removed, discount applied) — debounced; only the latest
///         snapshot is saved after the quiet period elapses.</item>
///   <item><b>Payment started</b> — immediate flush; the pre-payment
///         cart state must be persisted before the outcome resolves.</item>
///   <item><b>Session started</b> — creates the persistence row.</item>
///   <item><b>Session completed / cancelled</b> — marks the row
///         inactive and cancels any pending debounce.</item>
/// </list>
/// <para>
/// <b>Debounce strategy:</b> Rapid cart mutations (e.g., scanning
/// multiple items) coalesce into a single DB write after
/// <see cref="DebounceDelay"/>. Each new change resets the timer.
/// Payment-start and session-end bypass the debounce for safety.
/// </para>
/// <para>
/// Registered as a <b>singleton</b>. Disposes the debounce timer
/// and unsubscribes from the event bus.
/// </para>
/// </summary>
public interface IBillingAutoSaveService : IDisposable
{
    /// <summary>
    /// The quiet period between the last cart change and the actual
    /// database write. Defaults to 1 second.
    /// </summary>
    TimeSpan DebounceDelay { get; }

    /// <summary>
    /// <c>true</c> while a save operation is in flight.
    /// </summary>
    bool IsSaving { get; }
}
