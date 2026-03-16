using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Modules.MainShell.Models;

namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Provides a real <see cref="DashboardSummary"/> by querying the database.
/// </summary>
public class DashboardService(
    IDbContextFactory<AppDbContext> dbFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("DashboardService.GetSummaryAsync");
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var today = regional.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // ── Sales KPIs ──
        var todaySales = await db.Sales
            .Where(s => s.SaleDate >= today)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        var monthSales = await db.Sales
            .Where(s => s.SaleDate >= monthStart)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        // ── Inventory KPIs ──
        var productStats = await db.Products
            .Select(p => new { p.Quantity, p.MinStockLevel })
            .ToListAsync(ct);

        int totalProducts = productStats.Count;
        int outOfStock = productStats.Count(p => p.Quantity <= 0);
        int lowStock = productStats.Count(p => p.MinStockLevel > 0 && p.Quantity > 0 && p.Quantity <= p.MinStockLevel);

        // ── Orders ──
        var pendingOrders = await db.Orders
            .Where(o => o.Status == "Pending")
            .Select(o => new { o.DeliveryDate })
            .ToListAsync(ct);

        int overdueOrders = pendingOrders.Count(o => o.DeliveryDate.HasValue && o.DeliveryDate.Value.Date < today);

        // ── Receivables ──
        var receivables = await db.Debtors
            .SumAsync(d => d.TotalAmount - d.PaidAmount, ct);

        // ── Recent sales (top 10 today, fallback to latest 10) ──
        var recentSalesQuery = db.Sales
            .OrderByDescending(s => s.SaleDate)
            .Take(10)
            .Select(s => new RecentSaleItem(
                s.InvoiceNumber,
                s.SaleDate,
                s.TotalAmount,
                s.PaymentMethod,
                s.Items.Count));

        var recentSales = await recentSalesQuery.ToListAsync(ct);

        // ── Top products this month ──
        var topProducts = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= monthStart)
            .GroupBy(si => si.Product!.Name)
            .Select(g => new TopProductItem(
                g.Key,
                g.Sum(si => si.Quantity),
                g.Sum(si => si.UnitPrice * si.Quantity)))
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToListAsync(ct);

        return new DashboardSummary
        {
            TodaySales = todaySales.Sum(s => s.TotalAmount),
            TodayTransactions = todaySales.Count,
            ThisMonthSales = monthSales.Sum(s => s.TotalAmount),
            ThisMonthTransactions = monthSales.Count,
            TotalProducts = totalProducts,
            LowStockCount = lowStock,
            OutOfStockCount = outOfStock,
            PendingOrdersCount = pendingOrders.Count,
            OverdueOrdersCount = overdueOrders,
            OutstandingReceivables = receivables,
            RecentSales = recentSales,
            TopProducts = topProducts,
        };
    }
}
