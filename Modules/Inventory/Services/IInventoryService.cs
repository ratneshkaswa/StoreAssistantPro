using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inventory.Services;

public interface IInventoryService
{
    // ── Stock adjustments ──
    Task AdjustStockAsync(StockAdjustmentDto dto, CancellationToken ct = default);
    Task<int> BatchAdjustStockAsync(IReadOnlyList<StockAdjustmentDto> dtos, CancellationToken ct = default);
    Task<IReadOnlyList<StockAdjustment>> GetAdjustmentLogAsync(int productId, CancellationToken ct = default);
    Task<IReadOnlyList<StockAdjustment>> GetRecentAdjustmentsAsync(int count = 50, CancellationToken ct = default);

    // ── Low stock / out of stock ──
    Task<IReadOnlyList<Product>> GetLowStockProductsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetOutOfStockProductsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductVariant>> GetLowStockVariantsAsync(CancellationToken ct = default);

    // ── Stock value ──
    Task<decimal> GetTotalStockValueAsync(CancellationToken ct = default);

    // ── Stock movement history ──
    Task<IReadOnlyList<StockMovementEntry>> GetStockMovementHistoryAsync(int productId, CancellationToken ct = default);

    // ── Dead stock ──
    Task<IReadOnlyList<Product>> GetDeadStockAsync(int days = 90, CancellationToken ct = default);

    // ── Bulk import ──
    Task<int> ImportStockAsync(IReadOnlyList<Dictionary<string, string>> rows, int userId, CancellationToken ct = default);
}

public record StockAdjustmentDto(
    int ProductId,
    int? ProductVariantId,
    int NewQuantity,
    AdjustmentReason Reason,
    string? Notes,
    int UserId);

public record StockMovementEntry(
    DateTime Date,
    string Type,
    string Description,
    int QuantityChange,
    string? Reference);
