using StoreAssistantPro.Models.MultiStore;

namespace StoreAssistantPro.Modules.MultiStore.Services;

/// <summary>Store management service (#591-600).</summary>
public interface IStoreManagementService
{
    Task<IReadOnlyList<Store>> GetAllStoresAsync(CancellationToken ct = default);
    Task<Store?> GetStoreAsync(int storeId, CancellationToken ct = default);
    Task<Store> SaveStoreAsync(Store store, CancellationToken ct = default);
    Task DeleteStoreAsync(int storeId, CancellationToken ct = default);
    Task<Store?> GetActiveStoreAsync(CancellationToken ct = default);
    Task SwitchStoreAsync(int storeId, CancellationToken ct = default);
}

/// <summary>Inter-store transfer service (#601-606).</summary>
public interface IStockTransferService
{
    Task<StockTransfer> CreateTransferAsync(StockTransfer transfer, CancellationToken ct = default);
    Task<StockTransfer> ApproveTransferAsync(int transferId, int approverUserId, CancellationToken ct = default);
    Task<StockTransfer> CompleteTransferAsync(int transferId, CancellationToken ct = default);
    Task RejectTransferAsync(int transferId, string? reason = null, CancellationToken ct = default);
    Task<IReadOnlyList<StockTransfer>> GetPendingTransfersAsync(int storeId, CancellationToken ct = default);
    Task<IReadOnlyList<StockTransfer>> GetTransferHistoryAsync(int storeId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
}

/// <summary>Cloud sync service (#621-639).</summary>
public interface ISyncService
{
    Task<SyncStatus> GetSyncStatusAsync(int storeId, CancellationToken ct = default);
    Task SyncNowAsync(int storeId, CancellationToken ct = default);
    Task<IReadOnlyList<SyncStatus>> GetAllSyncStatusesAsync(CancellationToken ct = default);
    Task SetSyncScheduleAsync(int storeId, TimeSpan interval, CancellationToken ct = default);
}
