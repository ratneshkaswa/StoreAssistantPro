using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Persists billing sessions to the database so an operator can resume
/// after an application restart or crash.
/// <para>
/// <b>Operations:</b>
/// <list type="bullet">
///   <item><see cref="CreateAsync"/> — inserts a new active session row.</item>
///   <item><see cref="UpdateCartAsync"/> — overwrites serialized cart data
///         and bumps <c>LastUpdated</c>.</item>
///   <item><see cref="MarkCompletedAsync"/> — sets <c>IsActive = false</c>
///         when a bill finishes successfully.</item>
///   <item><see cref="MarkCancelledAsync"/> — sets <c>IsActive = false</c>
///         when a bill is abandoned.</item>
///   <item><see cref="GetActiveSessionAsync"/> — loads the most recent
///         active session for a user (startup resume check).</item>
///   <item><see cref="PurgeStaleSessionsAsync"/> — deletes inactive sessions
///         older than a threshold.</item>
/// </list>
/// </para>
/// <para>
/// Registered as a <b>singleton</b>. All database access is async via
/// <c>IDbContextFactory</c> — no long-lived <c>DbContext</c>.
/// </para>
/// </summary>
public interface IBillingSessionPersistenceService
{
    /// <summary>
    /// Creates a new persisted session row linked to the current user.
    /// </summary>
    /// <param name="sessionId">Correlation GUID matching the in-memory session.</param>
    /// <param name="serializedBillData">Initial JSON cart state.</param>
    /// <returns>The database <c>Id</c> of the new row.</returns>
    Task<int> CreateAsync(Guid sessionId, string serializedBillData, CancellationToken ct = default);

    /// <summary>
    /// Overwrites the serialized cart data for an active session.
    /// </summary>
    /// <param name="sessionId">Correlation GUID of the session to update.</param>
    /// <param name="serializedBillData">Updated JSON cart state.</param>
    Task UpdateCartAsync(Guid sessionId, string serializedBillData, CancellationToken ct = default);

    /// <summary>
    /// Marks the session as completed (<c>IsActive = false</c>).
    /// </summary>
    Task MarkCompletedAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Marks the session as cancelled (<c>IsActive = false</c>).
    /// </summary>
    Task MarkCancelledAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Returns the most recent active session for the given user, or
    /// <c>null</c> if no resumable session exists.
    /// </summary>
    Task<BillingSession?> GetActiveSessionAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Force-cancels all <b>active</b> sessions whose <c>LastUpdated</c>
    /// is older than <paramref name="olderThan"/>. These are sessions left
    /// behind by crashes or unclean shutdowns that are too old to resume
    /// safely.
    /// </summary>
    /// <returns>Number of sessions archived (deactivated).</returns>
    Task<int> ArchiveStaleActiveSessionsAsync(TimeSpan olderThan, CancellationToken ct = default);

    /// <summary>
    /// Deletes all inactive sessions whose <c>LastUpdated</c> is older
    /// than <paramref name="olderThan"/>.
    /// </summary>
    /// <returns>Number of rows deleted.</returns>
    Task<int> PurgeStaleSessionsAsync(TimeSpan olderThan, CancellationToken ct = default);
}
