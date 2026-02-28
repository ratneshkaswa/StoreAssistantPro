namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Checks for a persisted billing session on startup and offers the
/// operator a choice to resume or discard it.
/// <para>
/// <b>Startup flow:</b>
/// </para>
/// <code>
///   Login completes
///     → IBillingResumeService.TryResumeAsync()
///       → query DB for active session (by user)
///         → none found? → purge stale rows, return NoSession
///         → found? → prompt "Resume or Discard?"
///           → Resume  → start billing session
///                     → (event chain) SmartBillingModeService switches to Billing
///                     → (event chain) FocusLockService acquires focus lock
///                     → restore cart via IBillingSessionRestoreService
///                     → verify mode + focus lock active
///                     → return Resumed with restored cart
///           → Discard → mark cancelled, return Discarded
///     → show MainWindow (with or without restored cart)
/// </code>
/// <para>
/// Registered as a <b>singleton</b>. No UI dependencies — uses
/// <see cref="Core.Services.IDialogService"/> for the prompt.
/// </para>
/// </summary>
public interface IBillingResumeService
{
    /// <summary>
    /// Checks whether the current user has an active persisted billing
    /// session. If found, prompts the operator to resume or discard.
    /// <para>
    /// On resume, the full integration chain is triggered:
    /// <list type="number">
    ///   <item><see cref="IBillingSessionService.StartSessionAsync"/> →
    ///         publishes <c>BillingSessionStartedEvent</c>.</item>
    ///   <item><see cref="ISmartBillingModeService"/> reacts → switches
    ///         to <c>OperationalMode.Billing</c>.</item>
    ///   <item><see cref="Core.Services.IFocusLockService"/> reacts →
    ///         acquires focus lock, blocking manual navigation.</item>
    ///   <item><see cref="IBillingSessionRestoreService"/> validates and
    ///         recalculates cart items from the persisted JSON.</item>
    /// </list>
    /// The returned <see cref="ResumeResult"/> includes verification
    /// that both mode and focus lock are active.
    /// </para>
    /// </summary>
    Task<ResumeResult> TryResumeAsync(CancellationToken ct = default);
}
