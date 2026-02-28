using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// EF Core implementation of <see cref="IBillingSessionPersistenceService"/>.
/// <para>
/// Uses <see cref="IDbContextFactory{TContext}"/> for short-lived contexts
/// on every operation — safe for singleton lifetime.
/// </para>
/// <para>
/// The <see cref="ResolveUserIdAsync"/> helper maps the current
/// <see cref="ISessionService.CurrentUserType"/> to a
/// <see cref="UserCredential.Id"/> via the unique <c>UserType</c> index.
/// </para>
/// </summary>
public class BillingSessionPersistenceService(
    IDbContextFactory<AppDbContext> contextFactory,
    ISessionService session,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf,
    ILogger<BillingSessionPersistenceService> logger) : IBillingSessionPersistenceService
{
    // ── Create ─────────────────────────────────────────────────────

    public async Task<int> CreateAsync(
        Guid sessionId, string serializedBillData, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BillingSessionPersistence.CreateAsync");
        await using var db = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var userId = await ResolveUserIdAsync(db, ct).ConfigureAwait(false);
        var now = regional.Now;

        var entity = new BillingSession
        {
            SessionId = sessionId,
            UserId = userId,
            IsActive = true,
            SerializedBillData = serializedBillData,
            CreatedTime = now,
            LastUpdated = now
        };

        db.BillingSessions.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Created billing session {SessionId} (row {Id}) for user {UserId}",
            sessionId, entity.Id, userId);

        return entity.Id;
    }

    // ── Update cart ────────────────────────────────────────────────

    public async Task UpdateCartAsync(
        Guid sessionId, string serializedBillData, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BillingSessionPersistence.UpdateCartAsync");
        await using var db = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await db.BillingSessions
            .FirstOrDefaultAsync(b => b.SessionId == sessionId && b.IsActive, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            logger.LogWarning(
                "UpdateCart found no active session for {SessionId}", sessionId);
            return;
        }

        entity.SerializedBillData = serializedBillData;
        entity.LastUpdated = regional.Now;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Mark completed ─────────────────────────────────────────────

    public async Task MarkCompletedAsync(Guid sessionId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BillingSessionPersistence.MarkCompletedAsync");
        await DeactivateAsync(sessionId, ct).ConfigureAwait(false);

        logger.LogInformation("Billing session {SessionId} marked completed", sessionId);
    }

    // ── Mark cancelled ─────────────────────────────────────────────

    public async Task MarkCancelledAsync(Guid sessionId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BillingSessionPersistence.MarkCancelledAsync");
        await DeactivateAsync(sessionId, ct).ConfigureAwait(false);

        logger.LogInformation("Billing session {SessionId} marked cancelled", sessionId);
    }

    // ── Get active session (for resume) ────────────────────────────

    public async Task<BillingSession?> GetActiveSessionAsync(
        int userId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BillingSessionPersistence.GetActiveSessionAsync");
        await using var db = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await db.BillingSessions
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.IsActive)
            .OrderByDescending(b => b.LastUpdated)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    // ── Purge stale sessions ───────────────────────────────────────

    public async Task<int> ArchiveStaleActiveSessionsAsync(
        TimeSpan olderThan, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BillingSessionPersistence.ArchiveStaleActiveSessionsAsync");
        await using var db = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = regional.Now - olderThan;

        var stale = await db.BillingSessions
            .Where(b => b.IsActive && b.LastUpdated < cutoff)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (stale.Count == 0)
            return 0;

        var now = regional.Now;
        foreach (var session in stale)
        {
            session.IsActive = false;
            session.LastUpdated = now;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Archived {Count} stale active billing session(s) older than {Cutoff:u}",
            stale.Count, cutoff);

        return stale.Count;
    }

    public async Task<int> PurgeStaleSessionsAsync(
        TimeSpan olderThan, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BillingSessionPersistence.PurgeStaleSessionsAsync");
        await using var db = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = regional.Now - olderThan;

        var stale = await db.BillingSessions
            .Where(b => !b.IsActive && b.LastUpdated < cutoff)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (stale.Count == 0)
            return 0;

        db.BillingSessions.RemoveRange(stale);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Purged {Count} stale billing session(s) older than {Cutoff:u}",
            stale.Count, cutoff);

        return stale.Count;
    }

    // ── Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Sets <c>IsActive = false</c> and stamps <c>LastUpdated</c> for
    /// the given session.
    /// </summary>
    private async Task DeactivateAsync(Guid sessionId, CancellationToken ct)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await db.BillingSessions
            .FirstOrDefaultAsync(b => b.SessionId == sessionId && b.IsActive, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            logger.LogWarning(
                "Deactivate found no active session for {SessionId}", sessionId);
            return;
        }

        entity.IsActive = false;
        entity.LastUpdated = regional.Now;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps <see cref="ISessionService.CurrentUserType"/> to the
    /// <see cref="UserCredential.Id"/> via the unique <c>UserType</c> index.
    /// </summary>
    private async Task<int> ResolveUserIdAsync(AppDbContext db, CancellationToken ct)
    {
        var userType = session.CurrentUserType;

        var userId = await db.UserCredentials
            .Where(u => u.UserType == userType)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return userId != 0
            ? userId
            : throw new InvalidOperationException(
                $"No UserCredential found for UserType '{userType}'.");
    }
}
