using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inventory.Services;

public interface IInventoryService
{
    // ── Stock adjustments ──
    Task AdjustStockAsync(StockAdjustmentDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<StockAdjustment>> GetAdjustmentLogAsync(int productId, CancellationToken ct = default);
    Task<IReadOnlyList<StockAdjustment>> GetRecentAdjustmentsAsync(int count = 50, CancellationToken ct = default);

    // ── Low stock / out of stock ──
    Task<IReadOnlyList<Product>> GetLowStockProductsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetOutOfStockProductsAsync(CancellationToken ct = default);

    // ── Stock value ──
    Task<decimal> GetTotalStockValueAsync(CancellationToken ct = default);
}

public record StockAdjustmentDto(
    int ProductId,
    int? ProductVariantId,
    int NewQuantity,
    AdjustmentReason Reason,
    string? Notes,
    int UserId);
