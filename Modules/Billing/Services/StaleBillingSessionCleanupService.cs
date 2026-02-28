using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Two-phase stale billing session cleanup:
/// <list type="number">
///   <item><b>Archive:</b> Force-cancel active sessions whose
///         <c>LastUpdated</c> exceeds <see cref="StaleActiveThreshold"/>
///         (default 24 h). Prevents operators from resuming a bill that
///         is days old after a crash.</item>
///   <item><b>Purge:</b> Delete inactive rows older than
///         <see cref="InactiveRetentionPeriod"/> (default 7 days).
///         Keeps the table lean without losing recent audit history.</item>
/// </list>
/// <para>
/// <b>Startup:</b> <see cref="RunCleanupAsync"/> is called once before
/// <see cref="IBillingResumeService.TryResumeAsync"/> so the resume
/// prompt never offers stale sessions.
/// </para>
/// <para>
/// <b>Background timer:</b> An optional periodic timer repeats the
/// cleanup every <see cref="StaleActiveThreshold"/> while the app is
/// running, catching sessions that go stale during a long shift.
/// </para>
/// </summary>
public sealed class StaleBillingSessionCleanupService : IStaleBillingSessionCleanupService
{
    private readonly IBillingSessionPersistenceService _persistence;
    private readonly IPerformanceMonitor _perf;
    private readonly ILogger<StaleBillingSessionCleanupService> _logger;
    private Timer? _timer;

    /// <summary>Default: 24 hours.</summary>
    private static readonly TimeSpan DefaultStaleActiveThreshold = TimeSpan.FromHours(24);

    /// <summary>Default: 7 days.</summary>
    private static readonly TimeSpan DefaultInactiveRetention = TimeSpan.FromDays(7);

    public StaleBillingSessionCleanupService(
        IBillingSessionPersistenceService persistence,
        IPerformanceMonitor perf,
        ILogger<StaleBillingSessionCleanupService> logger)
        : this(persistence, perf, logger,
               DefaultStaleActiveThreshold,
               DefaultInactiveRetention,
               enableTimer: true)
    {
    }

    /// <summary>
    /// Test-friendly constructor with explicit thresholds and optional
    /// timer control.
    /// </summary>
    public StaleBillingSessionCleanupService(
        IBillingSessionPersistenceService persistence,
        IPerformanceMonitor perf,
        ILogger<StaleBillingSessionCleanupService> logger,
        TimeSpan staleActiveThreshold,
        TimeSpan inactiveRetentionPeriod,
        bool enableTimer = true)
    {
        _persistence = persistence;
        _perf = perf;
        _logger = logger;
        StaleActiveThreshold = staleActiveThreshold;
        InactiveRetentionPeriod = inactiveRetentionPeriod;

        if (enableTimer)
            StartTimer();
    }

    // ── Configuration ──────────────────────────────────────────────

    public TimeSpan StaleActiveThreshold { get; }
    public TimeSpan InactiveRetentionPeriod { get; }

    // ── Core logic ─────────────────────────────────────────────────

    public async Task<(int Archived, int Purged)> RunCleanupAsync(
        CancellationToken ct = default)
    {
        using var _ = _perf.BeginScope("StaleBillingSessionCleanup.RunCleanupAsync");

        // Phase 1: archive stale active sessions
        int archived;
        try
        {
            archived = await _persistence
                .ArchiveStaleActiveSessionsAsync(StaleActiveThreshold, ct)
                .ConfigureAwait(false);

            if (archived > 0)
                _logger.LogInformation(
                    "Archived {Count} stale active session(s) older than {Threshold}",
                    archived, StaleActiveThreshold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive stale active sessions");
            archived = 0;
        }

        // Phase 2: purge old inactive rows
        int purged;
        try
        {
            purged = await _persistence
                .PurgeStaleSessionsAsync(InactiveRetentionPeriod, ct)
                .ConfigureAwait(false);

            if (purged > 0)
                _logger.LogInformation(
                    "Purged {Count} inactive session(s) older than {Retention}",
                    purged, InactiveRetentionPeriod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purge inactive sessions");
            purged = 0;
        }

        return (archived, purged);
    }

    // ── Background timer ───────────────────────────────────────────

    private void StartTimer()
    {
        // First tick after the threshold elapses; repeats at same interval.
        _timer = new Timer(
            OnTimerElapsed,
            state: null,
            dueTime: StaleActiveThreshold,
            period: StaleActiveThreshold);
    }

    private void OnTimerElapsed(object? state)
    {
        _ = RunCleanupAsync(CancellationToken.None);
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
