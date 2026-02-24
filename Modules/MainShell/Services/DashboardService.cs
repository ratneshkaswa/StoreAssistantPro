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
    public async Task<DashboardSummary> GetSummaryAsync()
    {
        using var _ = perf.BeginScope("DashboardService.GetSummaryAsync");
        var today = regional.Now.Date;
        var products = (await productService.GetAllAsync().ConfigureAwait(false)).ToList();
        var todaysSales = (await salesService.GetSalesByDateRangeAsync(
            today, today.AddDays(1)).ConfigureAwait(false)).ToList();

        var lowStockProducts = products
            .Where(p => p.Quantity <= 5)
            .OrderBy(p => p.Quantity)
            .Take(10)
            .ToList();

        var recentSales = todaysSales
            .OrderByDescending(s => s.SaleDate)
            .Take(10)
            .ToList();

        return new DashboardSummary(
            TotalProducts: products.Count,
            LowStockCount: lowStockProducts.Count,
            TodaysSales: todaysSales.Sum(s => s.TotalAmount),
            TodaysTransactions: todaysSales.Count,
            RecentSales: recentSales,
            LowStockProducts: lowStockProducts);
    }
}
