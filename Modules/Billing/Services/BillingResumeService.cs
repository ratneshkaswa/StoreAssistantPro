using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Startup-time service that detects a persisted billing session from a
/// previous run, lets the operator resume or discard it, and — on
/// resume — orchestrates the full integration chain:
/// <para>
/// <b>Resume chain (all synchronous via <see cref="Core.Events.IEventBus"/>):</b>
/// </para>
/// <list type="number">
///   <item><see cref="IBillingSessionService.StartSessionAsync"/> →
///         publishes <c>BillingSessionStartedEvent</c>.</item>
///   <item><see cref="SmartBillingModeService"/> reacts → calls
///         <see cref="IBillingModeService.StartBillingAsync"/> →
///         publishes <c>OperationalModeChangedEvent(Billing)</c>.</item>
///   <item><see cref="IFocusLockService"/> reacts → calls
///         <c>Acquire("Billing")</c> → <c>IsFocusLocked = true</c>.
///         All sidebar navigation and side panels are blocked.</item>
///   <item><see cref="IBillingSessionRestoreService"/> validates and
///         recalculates cart items from the persisted JSON.</item>
/// </list>
/// <para>
/// After the chain completes, mode and focus lock are verified to catch
/// broken event wiring early.
/// </para>
/// </summary>
public class BillingResumeService(
    IDbContextFactory<AppDbContext> contextFactory,
    ISessionService session,
    IBillingSessionPersistenceService persistence,
    IBillingSessionService billingSession,
    IBillingSessionRestoreService restoreService,
    IBillingModeService modeService,
    IFocusLockService focusLock,
    IDialogService dialogService,
    IPerformanceMonitor perf,
    ILogger<BillingResumeService> logger) : IBillingResumeService
{
    /// <summary>Stale completed/cancelled sessions older than 7 days are purged.</summary>
    private static readonly TimeSpan PurgeThreshold = TimeSpan.FromDays(7);

    public async Task<ResumeResult> TryResumeAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BillingResumeService.TryResumeAsync");

        var userId = await ResolveUserIdAsync(ct).ConfigureAwait(false);
        if (userId == 0)
        {
            logger.LogWarning("Cannot check for resumable session — user not found");
            return ResumeResult.NoSession();
        }

        var activeSession = await persistence.GetActiveSessionAsync(userId, ct)
            .ConfigureAwait(false);

        if (activeSession is null)
        {
            FireAndForgetPurge(ct);
            logger.LogDebug("No active billing session found for user {UserId}", userId);
            return ResumeResult.NoSession();
        }

        logger.LogInformation(
            "Found resumable billing session {SessionId} (last updated {LastUpdated:u})",
            activeSession.SessionId, activeSession.LastUpdated);

        // ── Prompt ─────────────────────────────────────────────────
        var shouldResume = dialogService.ShowResumeBillingDialog(
            activeSession, session.CurrentUserType);

        if (!shouldResume)
        {
            await persistence.MarkCancelledAsync(activeSession.SessionId, ct)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Billing session {SessionId} discarded by user", activeSession.SessionId);

            FireAndForgetPurge(ct);
            return ResumeResult.Discarded(activeSession);
        }

        // ── Resume: start session → event chain → mode + focus lock ─
        await billingSession.StartSessionAsync().ConfigureAwait(false);

        // By this point the synchronous event chain has executed:
        //   BillingSessionStartedEvent
        //     → SmartBillingModeService.OnSessionStartedAsync
        //       → BillingModeService.StartBillingAsync
        //         → OperationalModeChangedEvent(Billing)
        //           → FocusLockService.Acquire("Billing")

        // ── Restore cart data ──────────────────────────────────────
        var restoredCart = await restoreService.RestoreAsync(activeSession, ct)
            .ConfigureAwait(false);

        if (restoredCart is null)
        {
            logger.LogWarning(
                "Cart restore returned null for session {SessionId} — " +
                "billing mode is active but cart is empty",
                activeSession.SessionId);
        }
        else if (restoredCart.HasWarnings)
        {
            logger.LogWarning(
                "Restored session {SessionId} with {Skipped} skipped item(s)",
                activeSession.SessionId, restoredCart.SkippedItems.Count);
        }

        // ── Post-resume verification ───────────────────────────────
        var modeActive = modeService.CurrentMode == OperationalMode.Billing;
        var lockActive = focusLock.IsFocusLocked;
        var integrationHealthy = modeActive && lockActive;

        if (!integrationHealthy)
        {
            logger.LogError(
                "Post-resume verification FAILED for session {SessionId}: " +
                "Mode={Mode} (expected Billing), FocusLocked={Locked} (expected true). " +
                "The event chain may be broken — check SmartBillingModeService and FocusLockService subscriptions.",
                activeSession.SessionId, modeService.CurrentMode, focusLock.IsFocusLocked);
        }
        else
        {
            logger.LogInformation(
                "Billing session {SessionId} fully resumed — " +
                "mode=Billing, focusLock=active, cart={ItemCount} items",
                activeSession.SessionId, restoredCart?.Items.Count ?? 0);
        }

        return ResumeResult.Resumed(activeSession, restoredCart, integrationHealthy);
    }

    /// <summary>
    /// Kicks off stale-session cleanup on a background thread.
    /// Failures are swallowed and logged by the persistence service.
    /// </summary>
    private void FireAndForgetPurge(CancellationToken ct) =>
        Task.Run(() => persistence.PurgeStaleSessionsAsync(PurgeThreshold, ct), ct);

    /// <summary>
    /// Maps <see cref="ISessionService.CurrentUserType"/> to a
    /// <see cref="UserCredential.Id"/>.
    /// Returns 0 if no matching credential is found.
    /// </summary>
    private async Task<int> ResolveUserIdAsync(CancellationToken ct)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        var userType = session.CurrentUserType;

        return await db.UserCredentials
            .Where(u => u.UserType == userType)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}
