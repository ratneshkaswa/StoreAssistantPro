using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Sales.Events;
using StoreAssistantPro.Modules.Sales.Models;

namespace StoreAssistantPro.Modules.Sales.Services;

/// <summary>
/// Replays offline-queued bills to the server when connectivity is
/// restored.
/// <para>
/// <b>Sync flow per bill:</b>
/// <code>
/// 1. bill.Status = Syncing          → UpdateAsync
/// 2. ISalesService.CreateSaleAsync   → Transaction + idempotency
/// 3a. Success → RemoveAsync          → file deleted
/// 3b. Failure → bill.Status = Failed → UpdateAsync (retried next cycle)
/// </code>
/// </para>
/// <para>
/// <b>Concurrency:</b> A <see cref="SemaphoreSlim"/>(1,1) ensures
/// only one sync batch runs at a time. If connectivity drops again
/// mid-sync, remaining bills are left in PendingSync/Failed and
/// will be picked up on the next restoration.
/// </para>
/// </summary>
public sealed class OfflineSyncService : IOfflineSyncService
{
    private readonly IOfflineBillingQueue _queue;
    private readonly ISalesService _salesService;
    private readonly IEventBus _eventBus;
    private readonly IStatusBarService _statusBar;
    private readonly IRegionalSettingsService _regional;
    private readonly IOfflineModeService _offlineMode;
    private readonly ILogger<OfflineSyncService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public OfflineSyncService(
        IOfflineBillingQueue queue,
        ISalesService salesService,
        IEventBus eventBus,
        IStatusBarService statusBar,
        IRegionalSettingsService regional,
        IOfflineModeService offlineMode,
        ILogger<OfflineSyncService> logger)
    {
        _queue = queue;
        _salesService = salesService;
        _eventBus = eventBus;
        _statusBar = statusBar;
        _regional = regional;
        _offlineMode = offlineMode;
        _logger = logger;

        _eventBus.Subscribe<OfflineModeChangedEvent>(OnModeChangedAsync);
    }

    // ── Public state ───────────────────────────────────────────────

    public bool IsSyncing => _gate.CurrentCount == 0;

    // ── Event handler ──────────────────────────────────────────────

    private async Task OnModeChangedAsync(OfflineModeChangedEvent e)
    {
        if (e.IsOffline)
            return;

        // Connection restored — fire-and-forget sync.
        // Errors are handled per-bill inside SyncPendingAsync.
        await SyncPendingAsync().ConfigureAwait(false);
    }

    // ── Core sync logic ────────────────────────────────────────────

    public async Task<int> SyncPendingAsync(CancellationToken ct = default)
    {
        if (!await _gate.WaitAsync(TimeSpan.Zero, ct).ConfigureAwait(false))
        {
            _logger.LogDebug("Sync already in progress — skipping");
            return 0;
        }

        try
        {
            var pending = await _queue.GetPendingAsync(ct).ConfigureAwait(false);

            if (pending.Count == 0)
                return 0;

            _logger.LogInformation(
                "Starting offline sync — {Count} bill(s) to process", pending.Count);
            _statusBar.Post($"🔄 Syncing {pending.Count} offline bill(s)…",
                TimeSpan.FromSeconds(10));

            var synced = 0;

            foreach (var bill in pending)
            {
                // Bail early if we went offline again.
                if (_offlineMode.IsOffline)
                {
                    _logger.LogWarning(
                        "Connection lost during sync — aborting with {Synced}/{Total} synced",
                        synced, pending.Count);
                    break;
                }

                if (await TrySyncBillAsync(bill, ct).ConfigureAwait(false))
                    synced++;
            }

            if (synced > 0)
            {
                _logger.LogInformation(
                    "Offline sync completed — {Synced}/{Total} bill(s) synced",
                    synced, pending.Count);
                _statusBar.Post($"✅ Synced {synced} offline bill(s)");
            }

            return synced;
        }
        finally
        {
            _gate.Release();
        }
    }

    // ── Per-bill sync ──────────────────────────────────────────────

    private async Task<bool> TrySyncBillAsync(
        OfflineBill bill, CancellationToken ct)
    {
        // 1. Mark as syncing
        bill.Status = OfflineBillStatus.Syncing;
        bill.SyncAttemptCount++;
        bill.LastSyncAttempt = _regional.Now;
        await _queue.UpdateAsync(bill, ct).ConfigureAwait(false);

        // 2. Build Sale entity from snapshot
        var snapshot = bill.Sale;
        var sale = new Sale
        {
            IdempotencyKey = bill.IdempotencyKey,
            SaleDate = snapshot.SaleDate,
            TotalAmount = snapshot.TotalAmount,
            PaymentMethod = snapshot.PaymentMethod,
            DiscountType = snapshot.DiscountType,
            DiscountValue = snapshot.DiscountValue,
            DiscountAmount = snapshot.DiscountAmount,
            DiscountReason = snapshot.DiscountReason,
            Items = snapshot.Items.Select(i => new SaleItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        // 3. Push to server (transaction-safe, idempotent)
        var result = await _salesService.CreateSaleAsync(sale, ct)
            .ConfigureAwait(false);

        if (result.Succeeded)
        {
            // 4a. Success — remove from queue, publish event
            await _queue.RemoveAsync(bill.IdempotencyKey, ct)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Synced offline bill {Key} → Sale {SaleId}",
                bill.IdempotencyKey, result.Value);

            await PublishSafeAsync(
                new SaleCompletedEvent(result.Value, snapshot.TotalAmount))
                .ConfigureAwait(false);

            return true;
        }

        // 4b. Failure — mark as failed, record error
        bill.Status = OfflineBillStatus.Failed;
        bill.LastError = result.ErrorMessage;
        await _queue.UpdateAsync(bill, ct).ConfigureAwait(false);

        _logger.LogWarning(
            "Failed to sync offline bill {Key}: {Error}",
            bill.IdempotencyKey, result.ErrorMessage);

        return false;
    }

    // ── Internals ──────────────────────────────────────────────────

    private async Task PublishSafeAsync<TEvent>(TEvent @event)
        where TEvent : IEvent
    {
        try
        {
            await _eventBus.PublishAsync(@event).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish {EventType} — event swallowed",
                typeof(TEvent).Name);
        }
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        _eventBus.Unsubscribe<OfflineModeChangedEvent>(OnModeChangedAsync);
        _gate.Dispose();
    }
}
