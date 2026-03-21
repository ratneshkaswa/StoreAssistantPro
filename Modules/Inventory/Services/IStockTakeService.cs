using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inventory.Services;

/// <summary>
/// Manages physical stock count sessions (#69).
/// </summary>
public interface IStockTakeService
{
    /// <summary>Start a new stock take — snapshots all active product quantities.</summary>
    Task<StockTake> StartAsync(string? notes, int userId, CancellationToken ct = default);

    /// <summary>Get a stock take by ID with all items.</summary>
    Task<StockTake?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Get recent stock take sessions.</summary>
    Task<IReadOnlyList<StockTake>> GetRecentAsync(int count = 20, CancellationToken ct = default);

    /// <summary>Record a counted quantity for one item.</summary>
    Task UpdateCountAsync(int stockTakeItemId, int countedQty, CancellationToken ct = default);

    /// <summary>Complete the stock take — generates stock adjustments for all discrepancies.</summary>
    Task<StockTakeResult> CompleteAsync(int stockTakeId, int userId, CancellationToken ct = default);

    /// <summary>Cancel an in-progress stock take.</summary>
    Task CancelAsync(int stockTakeId, CancellationToken ct = default);
}

public record StockTakeResult(int TotalItems, int Discrepancies, int Adjusted);
