using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.MainShell.Models;

/// <summary>
/// Module-local DTO carrying aggregated dashboard metrics.
/// Keeps MainWorkspaceViewModel decoupled from Products and Sales modules.
/// </summary>
public sealed record DashboardSummary(
    int TotalProducts,
    int LowStockCount,
    int OutOfStockCount,
    int OverstockCount,
    decimal InventoryValue,
    decimal InventoryValueAtSale,
    decimal TodaysSales,
    int TodaysTransactions,
    decimal TodaysAverageSale,
    IReadOnlyList<Sale> RecentSales,
    IReadOnlyList<Product> LowStockProducts,
    IReadOnlyList<BrandLowStockCount> LowStockByBrand,
    IReadOnlyList<Product> OutOfStockProducts,
    IReadOnlyList<BrandInventoryValue> InventoryValueByBrand,
    decimal TodaysTotalDiscount,
    IReadOnlyList<PaymentMethodSales> SalesByPaymentMethod,
    IReadOnlyList<TopSellingProduct> TopSellingProducts);

/// <summary>
/// Low-stock product count for a single brand.
/// </summary>
public sealed record BrandLowStockCount(string BrandName, int Count);

/// <summary>
/// Inventory value for a single brand.
/// </summary>
public sealed record BrandInventoryValue(string BrandName, decimal Value);

/// <summary>
/// Today's sales total for a single payment method.
/// </summary>
public sealed record PaymentMethodSales(string PaymentMethod, decimal Total, int Count);

/// <summary>
/// Top selling product by quantity sold today.
/// </summary>
public sealed record TopSellingProduct(string ProductName, int QuantitySold, decimal Revenue);
