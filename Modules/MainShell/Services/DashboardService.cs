using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Sales.Services;

namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Aggregates data from Products and Sales modules into a single
/// <see cref="DashboardSummary"/>. Cross-module coupling is isolated
/// in this service so ViewModels remain module-independent.
/// </summary>
public class DashboardService(
    IProductService productService,
    ISalesService salesService,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : IDashboardService
{
    private const int LowStockThreshold = 10;

    public async Task<DashboardSummary> GetSummaryAsync()
    {
        using var _ = perf.BeginScope("DashboardService.GetSummaryAsync");
        var today = regional.Now.Date;
        var products = (await productService.GetAllAsync().ConfigureAwait(false)).ToList();
        var todaysSales = (await salesService.GetSalesByDateRangeAsync(
            today, today.AddDays(1)).ConfigureAwait(false)).ToList();

        var lowStockProducts = products
            .Where(p => p.Quantity <= LowStockThreshold)
            .OrderBy(p => p.Quantity)
            .Take(10)
            .ToList();

        var recentSales = todaysSales
            .OrderByDescending(s => s.SaleDate)
            .Take(10)
            .ToList();

        var todaysTotal = todaysSales.Sum(s => s.TotalAmount);
        var todaysCount = todaysSales.Count;

        var lowStockByBrand = products
            .Where(p => p.IsActive && p.IsLowStock)
            .GroupBy(p => p.Brand?.Name ?? "No Brand")
            .Select(g => new BrandLowStockCount(g.Key, g.Count()))
            .OrderByDescending(b => b.Count)
            .ToList();

        var outOfStockProducts = products
            .Where(p => p.IsActive && p.Quantity == 0)
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();

        var inventoryValueByBrand = products
            .Where(p => p.IsActive)
            .GroupBy(p => p.Brand?.Name ?? "No Brand")
            .Select(g => new BrandInventoryValue(g.Key, g.Sum(p => p.CostPrice * p.Quantity)))
            .OrderByDescending(b => b.Value)
            .ToList();

        var salesByPaymentMethod = todaysSales
            .GroupBy(s => s.PaymentMethod)
            .Select(g => new PaymentMethodSales(g.Key, g.Sum(s => s.TotalAmount), g.Count()))
            .OrderByDescending(p => p.Total)
            .ToList();

        var topSellingProducts = todaysSales
            .SelectMany(s => s.Items)
            .GroupBy(i => i.Product?.Name ?? "Unknown")
            .Select(g => new TopSellingProduct(g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.Quantity * i.UnitPrice)))
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToList();

        return new DashboardSummary(
            TotalProducts: products.Count,
            LowStockCount: lowStockProducts.Count,
            OutOfStockCount: products.Count(p => p.IsActive && p.Quantity == 0),
            OverstockCount: products.Count(p => p.IsActive && p.IsOverStock),
            InventoryValue: products.Where(p => p.IsActive).Sum(p => p.CostPrice * p.Quantity),
            InventoryValueAtSale: products.Where(p => p.IsActive).Sum(p => p.SalePrice * p.Quantity),
            TodaysSales: todaysTotal,
            TodaysTransactions: todaysCount,
            TodaysAverageSale: todaysCount > 0 ? todaysTotal / todaysCount : 0,
            RecentSales: recentSales,
            LowStockProducts: lowStockProducts,
            LowStockByBrand: lowStockByBrand,
            OutOfStockProducts: outOfStockProducts,
            InventoryValueByBrand: inventoryValueByBrand,
            TodaysTotalDiscount: todaysSales.Sum(s => s.DiscountAmount),
            SalesByPaymentMethod: salesByPaymentMethod,
            TopSellingProducts: topSellingProducts);
    }
}
