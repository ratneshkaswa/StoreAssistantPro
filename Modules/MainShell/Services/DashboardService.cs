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
    ISalesService salesService) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync()
    {
        var products = (await productService.GetAllAsync().ConfigureAwait(false)).ToList();
        var todaysSales = (await salesService.GetSalesByDateRangeAsync(
            DateTime.Today, DateTime.Today.AddDays(1)).ConfigureAwait(false)).ToList();

        return new DashboardSummary(
            TotalProducts: products.Count,
            LowStockCount: products.Count(p => p.Quantity <= 5),
            TodaysSales: todaysSales.Sum(s => s.TotalAmount),
            TodaysTransactions: todaysSales.Count);
    }
}
