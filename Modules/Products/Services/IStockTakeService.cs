namespace StoreAssistantPro.Modules.Products.Services;

/// <summary>
/// Coordinates stock take sessions — loading products, recording counts,
/// and applying discrepancy adjustments.
/// <para><b>Feature #69</b> — Stock take / physical count.</para>
/// </summary>
public interface IStockTakeService
{
    /// <summary>
    /// Loads all active products as stock take items with current system quantities.
    /// </summary>
    Task<IReadOnlyList<StockTakeItem>> LoadStockTakeItemsAsync(CancellationToken ct = default);

    /// <summary>
    /// Applies adjustments for all items with discrepancies, creating adjustment log entries.
    /// Returns the number of products adjusted.
    /// </summary>
    Task<int> ApplyStockTakeAsync(IReadOnlyList<StockTakeItem> items, CancellationToken ct = default);
}
