using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.MultiStore;

namespace StoreAssistantPro.Modules.MultiStore.Services;

public sealed class StoreManagementService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<StoreManagementService> logger) : IStoreManagementService
{
    private int? _activeStoreId;

    public async Task<IReadOnlyList<Store>> GetAllStoresAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Stores.OrderBy(s => s.Name).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<Store?> GetStoreAsync(int storeId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Stores.FindAsync([storeId], ct).ConfigureAwait(false);
    }

    public async Task<Store> SaveStoreAsync(Store store, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (store.Id == 0) { store.CreatedAt = DateTime.UtcNow; context.Stores.Add(store); }
        else context.Stores.Update(store);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Store saved: {Name}", store.Name);
        return store;
    }

    public async Task DeleteStoreAsync(int storeId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var store = await context.Stores.FindAsync([storeId], ct).ConfigureAwait(false);
        if (store is null) return;
        store.IsActive = false;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Store deactivated: {Id}", storeId);
    }

    public async Task<Store?> GetActiveStoreAsync(CancellationToken ct = default)
    {
        if (_activeStoreId is null) return null;
        return await GetStoreAsync(_activeStoreId.Value, ct).ConfigureAwait(false);
    }

    public Task SwitchStoreAsync(int storeId, CancellationToken ct = default)
    {
        _activeStoreId = storeId;
        logger.LogInformation("Switched to store {Id}", storeId);
        return Task.CompletedTask;
    }
}

public sealed class StockTransferService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<StockTransferService> logger) : IStockTransferService
{
    public async Task<StockTransfer> CreateTransferAsync(StockTransfer transfer, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        transfer.RequestedAt = DateTime.UtcNow;
        transfer.Status = "Requested";
        context.StockTransfers.Add(transfer);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Transfer created: {Product} from Store {From} to {To}", transfer.ProductId, transfer.FromStoreId, transfer.ToStoreId);
        return transfer;
    }

    public async Task<StockTransfer> ApproveTransferAsync(int transferId, int approverUserId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var transfer = await context.StockTransfers.FindAsync([transferId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Transfer {transferId} not found");
        transfer.Status = "Approved";
        transfer.ApprovedByUserId = approverUserId;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return transfer;
    }

    public async Task<StockTransfer> CompleteTransferAsync(int transferId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var transfer = await context.StockTransfers.FindAsync([transferId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Transfer {transferId} not found");
        transfer.Status = "Received";
        transfer.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return transfer;
    }

    public async Task RejectTransferAsync(int transferId, string? reason = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var transfer = await context.StockTransfers.FindAsync([transferId], ct).ConfigureAwait(false);
        if (transfer is null) return;
        transfer.Status = "Rejected";
        transfer.Notes = reason;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StockTransfer>> GetPendingTransfersAsync(int storeId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.StockTransfers
            .Where(t => (t.FromStoreId == storeId || t.ToStoreId == storeId) && t.Status == "Requested")
            .OrderByDescending(t => t.RequestedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StockTransfer>> GetTransferHistoryAsync(int storeId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.StockTransfers.Where(t => t.FromStoreId == storeId || t.ToStoreId == storeId);
        if (from.HasValue) query = query.Where(t => t.RequestedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.RequestedAt <= to.Value);
        return await query.OrderByDescending(t => t.RequestedAt).ToListAsync(ct).ConfigureAwait(false);
    }
}

public sealed class SyncService(ILogger<SyncService> logger) : ISyncService
{
    public Task<SyncStatus> GetSyncStatusAsync(int storeId, CancellationToken ct = default) =>
        Task.FromResult(new SyncStatus(storeId, $"Store {storeId}", DateTime.UtcNow, 0, false, null));

    public Task SyncNowAsync(int storeId, CancellationToken ct = default)
    {
        logger.LogInformation("Manual sync triggered for store {Id}", storeId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SyncStatus>> GetAllSyncStatusesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SyncStatus>>([]);

    public Task SetSyncScheduleAsync(int storeId, TimeSpan interval, CancellationToken ct = default)
    {
        logger.LogInformation("Sync schedule set: store {Id} every {Interval}", storeId, interval);
        return Task.CompletedTask;
    }
}
