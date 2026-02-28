namespace StoreAssistantPro.Modules.Products.Services;

/// <summary>
/// Lightweight aggregate snapshot of inventory metrics.
/// Feature #72 — Stock value report.
/// </summary>
public sealed record InventorySummary(
    int TotalProducts,
    int ActiveProducts,
    int TotalUnits,
    decimal TotalCostValue,
    decimal TotalSaleValue,
    int LowStockCount,
    int OutOfStockCount);
