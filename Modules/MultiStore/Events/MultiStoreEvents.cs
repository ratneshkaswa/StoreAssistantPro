using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.MultiStore;

namespace StoreAssistantPro.Modules.MultiStore.Events;

/// <summary>Published when a new store is added or updated.</summary>
public sealed class StoreUpdatedEvent(Store store) : IEvent
{
    public Store Store { get; } = store;
}

/// <summary>Published when a stock transfer is completed.</summary>
public sealed class StockTransferCompletedEvent(StockTransfer transfer) : IEvent
{
    public StockTransfer Transfer { get; } = transfer;
}

/// <summary>Published when inter-store sync completes.</summary>
public sealed class StoreSyncCompletedEvent(int storeId, int changesSynced) : IEvent
{
    public int StoreId { get; } = storeId;
    public int ChangesSynced { get; } = changesSynced;
}
