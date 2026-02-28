using StoreAssistantPro.Modules.Sales.Models;

namespace StoreAssistantPro.Modules.Sales.Services;

/// <summary>
/// Local file-based queue for bills created while the application is
/// offline. Bills are serialized as individual JSON files in a
/// dedicated directory and replayed to the server when connectivity
/// is restored.
/// <para>
/// <b>Storage layout:</b>
/// <code>
/// Documents/StoreAssistantPro/OfflineQueue/
///   {IdempotencyKey}.json   ← one file per queued bill
/// </code>
/// </para>
/// <para>
/// <b>Thread safety:</b> All file operations are serialized via an
/// async semaphore. The queue is safe to call from any thread.
/// </para>
/// <para>
/// <b>Idempotency:</b> The <see cref="OfflineBill.IdempotencyKey"/>
/// is used as the file name. Enqueuing the same key twice overwrites
/// the file — no duplicates are possible.
/// </para>
/// </summary>
public interface IOfflineBillingQueue
{
    /// <summary>
    /// Enqueues a bill for offline storage. If a bill with the same
    /// <see cref="OfflineBill.IdempotencyKey"/> already exists, it is
    /// overwritten.
    /// </summary>
    Task EnqueueAsync(OfflineBill bill, CancellationToken ct = default);

    /// <summary>
    /// Returns all bills currently in the queue, ordered by creation
    /// time (oldest first).
    /// </summary>
    Task<IReadOnlyList<OfflineBill>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns only bills with <see cref="OfflineBillStatus.PendingSync"/>
    /// or <see cref="OfflineBillStatus.Failed"/> status, ordered by
    /// creation time (oldest first).
    /// </summary>
    Task<IReadOnlyList<OfflineBill>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates an existing queued bill (e.g. to change status or
    /// record a sync attempt).
    /// </summary>
    Task UpdateAsync(OfflineBill bill, CancellationToken ct = default);

    /// <summary>
    /// Removes a synced bill from the queue after successful server
    /// persistence.
    /// </summary>
    Task RemoveAsync(Guid idempotencyKey, CancellationToken ct = default);

    /// <summary>
    /// Returns the number of bills currently in the queue.
    /// </summary>
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the number of bills with
    /// <see cref="OfflineBillStatus.PendingSync"/> status.
    /// </summary>
    Task<int> PendingCountAsync(CancellationToken ct = default);
}
