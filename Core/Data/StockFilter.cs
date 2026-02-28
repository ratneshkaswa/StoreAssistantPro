namespace StoreAssistantPro.Core.Data;

/// <summary>
/// Filter products by stock status relative to their MinStockLevel.
/// </summary>
public enum StockFilter
{
    All,
    InStock,
    LowStock,
    OutOfStock
}
