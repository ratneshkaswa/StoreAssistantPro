namespace StoreAssistantPro.Modules.Sales.Services;

/// <summary>
/// Replays offline-queued bills to the server database when
/// connectivity is restored.
/// <para>
/// <b>Trigger:</b> Subscribes to
/// <see cref="Core.Events.OfflineModeChangedEvent"/> with
/// <c>IsOffline = false</c>. When connectivity returns, all
/// <see cref="Models.OfflineBillStatus.PendingSync"/> and
/// <see cref="Models.OfflineBillStatus.Failed"/> bills are pushed
/// to the server via <see cref="ISalesService.CreateSaleAsync"/>.
/// </para>
/// <para>
/// <b>Idempotency:</b> Each bill carries an
/// <see cref="Models.OfflineBill.IdempotencyKey"/> that maps to
/// <see cref="Models.Sale.IdempotencyKey"/>. The server's unique
/// index prevents duplicate inserts even if a sync is retried.
/// </para>
/// <para>
/// <b>Retry:</b> Bills that fail to sync are marked
/// <see cref="Models.OfflineBillStatus.Failed"/> with the error
/// message recorded. They will be retried on the next connectivity
/// restoration or via <see cref="SyncPendingAsync"/>.
/// </para>
/// <para>
/// Registered as a <b>singleton</b>. Implements <see cref="IDisposable"/>
/// to unsubscribe from the event bus at shutdown.
/// </para>
/// </summary>
public interface IOfflineSyncService : IDisposable
{
    /// <summary>
    /// <c>true</c> while a sync batch is in progress.
    /// </summary>
    bool IsSyncing { get; }

    /// <summary>
    /// Manually triggers a sync of all pending bills. Called
    /// automatically when connectivity is restored, but can also be
    /// invoked on demand (e.g. from a "Retry Sync" button).
    /// </summary>
    /// <returns>
    /// The number of bills successfully synced in this batch.
    /// </returns>
    Task<int> SyncPendingAsync(CancellationToken ct = default);
}
