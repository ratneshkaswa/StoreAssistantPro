namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Runs at startup (and optionally on a periodic timer) to archive
/// abandoned billing sessions and purge old inactive rows.
/// <para>
/// <b>Two-phase cleanup:</b>
/// </para>
/// <list type="number">
///   <item><b>Archive stale active sessions:</b> Any session still marked
///         <c>IsActive = true</c> whose <c>LastUpdated</c> is older than
///         <see cref="StaleActiveThreshold"/> is force-cancelled. This
///         prevents accidental restoration of bills from days ago after
///         a crash or unclean shutdown.</item>
///   <item><b>Purge old inactive sessions:</b> Completed or cancelled
///         rows whose <c>LastUpdated</c> is older than
///         <see cref="InactiveRetentionPeriod"/> are deleted.</item>
/// </list>
/// <para>
/// Both thresholds are configurable via the constructor overload used
/// by tests. Production uses the defaults (24 hours / 7 days).
/// </para>
/// <para>
/// Registered as a <b>singleton</b>. Implements <see cref="IDisposable"/>
/// to stop the optional background timer.
/// </para>
/// </summary>
public interface IStaleBillingSessionCleanupService : IDisposable
{
    /// <summary>
    /// Active sessions older than this are force-cancelled.
    /// Default: 24 hours.
    /// </summary>
    TimeSpan StaleActiveThreshold { get; }

    /// <summary>
    /// Inactive (completed/cancelled) sessions older than this are deleted.
    /// Default: 7 days.
    /// </summary>
    TimeSpan InactiveRetentionPeriod { get; }

    /// <summary>
    /// Runs both cleanup phases. Safe to call multiple times.
    /// </summary>
    /// <returns>
    /// A tuple of (archived active count, purged inactive count).
    /// </returns>
    Task<(int Archived, int Purged)> RunCleanupAsync(CancellationToken ct = default);
}
