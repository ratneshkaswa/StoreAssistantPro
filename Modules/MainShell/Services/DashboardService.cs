using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Modules.Backup.Services;
using StoreAssistantPro.Modules.MainShell.Models;

namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Provides a real <see cref="DashboardSummary"/> by querying the database.
/// </summary>
public class DashboardService(
    IDbContextFactory<AppDbContext> dbFactory,
    IRegionalSettingsService regional,
    IBackupService backupService,
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

        // ── Today's returns (#389) ──
        var todayReturns = await db.SaleReturns
            .Where(r => r.ReturnDate >= today)
            .SumAsync(r => r.RefundAmount, ct);

        // ── Today's profit (#391) — revenue minus cost ──
        var todayCost = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= today)
            .SumAsync(si => si.Product!.CostPrice * si.Quantity, ct);

        var todaySalesTotal = todaySales.Sum(s => s.TotalAmount);
        var todayNet = todaySalesTotal - todayReturns;
        var todayTxCount = todaySales.Count;
        var todayProfit = todayNet - todayCost;
        var averageBill = todayTxCount > 0 ? todaySalesTotal / todayTxCount : 0;

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

        // ── Pending payments count (#397) ──
        var pendingPayments = await db.Debtors
            .Where(d => d.TotalAmount > d.PaidAmount)
            .Select(d => d.Phone)
            .Distinct()
            .CountAsync(ct);

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

        // ── Top products today (#394) ──
        var topProductsToday = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= today)
            .GroupBy(si => si.Product!.Name)
            .Select(g => new TopProductItem(
                g.Key,
                g.Sum(si => si.Quantity),
                g.Sum(si => si.UnitPrice * si.Quantity)))
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToListAsync(ct);

        // ── Last backup date (#332) ──
        DateTime? lastBackupDate = null;
        try { lastBackupDate = await backupService.GetLastBackupDateAsync(ct); }
        catch { /* non-critical — don't fail dashboard for backup check */ }

        // ── Monthly sales trend (#398) — daily totals for last 30 days ──
        var thirtyDaysAgo = today.AddDays(-29);
        var trendRaw = await db.Sales
            .Where(s => s.SaleDate >= thirtyDaysAgo)
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new DailySalesTrendItem(g.Key, g.Sum(s => s.TotalAmount), g.Count()))
            .OrderBy(t => t.Date)
            .ToListAsync(ct);

        // Fill missing days with zero
        var trendFilled = new List<DailySalesTrendItem>();
        for (var d = thirtyDaysAgo; d <= today; d = d.AddDays(1))
        {
            var existing = trendRaw.FirstOrDefault(t => t.Date.Date == d);
            trendFilled.Add(existing ?? new DailySalesTrendItem(d, 0, 0));
        }

        // ── Payment method breakdown (#401) ──
        var paymentBreakdown = await db.Sales
            .Where(s => s.SaleDate >= monthStart)
            .GroupBy(s => s.PaymentMethod)
            .Select(g => new PaymentMethodBreakdownItem(g.Key, g.Sum(s => s.TotalAmount), g.Count()))
            .OrderByDescending(p => p.Amount)
            .ToListAsync(ct);

        return new DashboardSummary
        {
            TodaySales = todaySalesTotal,
            TodayTransactions = todayTxCount,
            TodayReturns = todayReturns,
            TodayNetSales = todayNet,
            TodayProfit = todayProfit,
            AverageBillValue = averageBill,
            ThisMonthSales = monthSales.Sum(s => s.TotalAmount),
            ThisMonthTransactions = monthSales.Count,
            TotalProducts = totalProducts,
            LowStockCount = lowStock,
            OutOfStockCount = outOfStock,
            PendingOrdersCount = pendingOrders.Count,
            OverdueOrdersCount = overdueOrders,
            OutstandingReceivables = receivables,
            PendingPaymentsCount = pendingPayments,
            RecentSales = recentSales,
            TopProducts = topProducts,
            TopProductsToday = topProductsToday,
            DailySalesTrend = trendFilled,
            PaymentMethodBreakdown = paymentBreakdown,
            LastBackupDate = lastBackupDate,
        };
    }
    public async Task<DashboardSummary> GetSummaryForDateAsync
    {
        using var _ = perf.BeginScope("DashboardService.GetSummaryForDateAsync");
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var targetDate = date.Date;
        var monthStart = new DateTime(targetDate.Year, targetDate.Month, 1);
        var nextDay = targetDate.AddDays(1);

        // ── Sales KPIs for target date ──
        var daySales = await db.Sales
            .Where(s => s.SaleDate >= targetDate && s.SaleDate < nextDay)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        var monthSales = await db.Sales
            .Where(s => s.SaleDate >= monthStart && s.SaleDate < nextDay)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        var dayReturns = await db.SaleReturns
            .Where(r => r.ReturnDate >= targetDate && r.ReturnDate < nextDay)
            .SumAsync(r => r.RefundAmount, ct);

        var dayCost = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= targetDate && si.Sale.SaleDate < nextDay)
            .SumAsync(si => si.Product!.CostPrice * si.Quantity, ct);

        var daySalesTotal = daySales.Sum(s => s.TotalAmount);
        var dayNet = daySalesTotal - dayReturns;
        var dayTxCount = daySales.Count;
        var dayProfit = dayNet - dayCost;
        var avgBill = dayTxCount > 0 ? daySalesTotal / dayTxCount : 0;

        // ── Inventory KPIs (always current) ──
        var productStats = await db.Products
            .Select(p => new { p.Quantity, p.MinStockLevel })
            .ToListAsync(ct);

        int totalProducts = productStats.Count;
        int outOfStock = productStats.Count(p => p.Quantity <= 0);
        int lowStock = productStats.Count(p => p.MinStockLevel > 0 && p.Quantity > 0 && p.Quantity <= p.MinStockLevel);

        // ── Orders (current) ──
        var pendingOrders = await db.Orders
            .Where(o => o.Status == "Pending")
            .Select(o => new { o.DeliveryDate })
            .ToListAsync(ct);
        int overdueOrders = pendingOrders.Count(o => o.DeliveryDate.HasValue && o.DeliveryDate.Value.Date < targetDate);

        // ── Receivables (current) ──
        var receivables = await db.Debtors.SumAsync(d => d.TotalAmount - d.PaidAmount, ct);
        var pendingPayments = await db.Debtors
            .Where(d => d.TotalAmount > d.PaidAmount)
            .Select(d => d.Phone).Distinct().CountAsync(ct);

        // ── Recent sales for the date ──
        var recentSales = await db.Sales
            .Where(s => s.SaleDate >= targetDate && s.SaleDate < nextDay)
            .OrderByDescending(s => s.SaleDate)
            .Take(10)
            .Select(s => new RecentSaleItem(
                s.InvoiceNumber, s.SaleDate, s.TotalAmount, s.PaymentMethod, s.Items.Count))
            .ToListAsync(ct);

        // ── Top products for the date ──
        var topProducts = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= monthStart && si.Sale.SaleDate < nextDay)
            .GroupBy(si => si.Product!.Name)
            .Select(g => new TopProductItem(g.Key, g.Sum(si => si.Quantity), g.Sum(si => si.UnitPrice * si.Quantity)))
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToListAsync(ct);

        var topProductsDay = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= targetDate && si.Sale.SaleDate < nextDay)
            .GroupBy(si => si.Product!.Name)
            .Select(g => new TopProductItem(g.Key, g.Sum(si => si.Quantity), g.Sum(si => si.UnitPrice * si.Quantity)))
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToListAsync(ct);

        DateTime? lastBackupDate = null;
        try { lastBackupDate = await backupService.GetLastBackupDateAsync(ct); }
        catch { /* non-critical */ }

        // ── Monthly sales trend for the target month (#398) ──
        var trendRaw = await db.Sales
            .Where(s => s.SaleDate >= monthStart && s.SaleDate < nextDay)
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new DailySalesTrendItem(g.Key, g.Sum(s => s.TotalAmount), g.Count()))
            .OrderBy(t => t.Date)
            .ToListAsync(ct);

        var trendFilled = new List<DailySalesTrendItem>();
        for (var d = monthStart; d <= targetDate; d = d.AddDays(1))
        {
            var existing = trendRaw.FirstOrDefault(t => t.Date.Date == d);
            trendFilled.Add(existing ?? new DailySalesTrendItem(d, 0, 0));
        }

        // ── Payment method breakdown for the month (#401) ──
        var paymentBreakdown = await db.Sales
            .Where(s => s.SaleDate >= monthStart && s.SaleDate < nextDay)
            .GroupBy(s => s.PaymentMethod)
            .Select(g => new PaymentMethodBreakdownItem(g.Key, g.Sum(s => s.TotalAmount), g.Count()))
            .OrderByDescending(p => p.Amount)
            .ToListAsync(ct);

        return new DashboardSummary
        {
            TodaySales = daySalesTotal,
            TodayTransactions = dayTxCount,
            TodayReturns = dayReturns,
            TodayNetSales = dayNet,
            TodayProfit = dayProfit,
            AverageBillValue = avgBill,
            ThisMonthSales = monthSales.Sum(s => s.TotalAmount),
            ThisMonthTransactions = monthSales.Count,
            TotalProducts = totalProducts,
            LowStockCount = lowStock,
            OutOfStockCount = outOfStock,
            PendingOrdersCount = pendingOrders.Count,
            OverdueOrdersCount = overdueOrders,
            OutstandingReceivables = receivables,
            PendingPaymentsCount = pendingPayments,
            RecentSales = recentSales,
            TopProducts = topProducts,
            TopProductsToday = topProductsDay,
            DailySalesTrend = trendFilled,
            PaymentMethodBreakdown = paymentBreakdown,
            LastBackupDate = lastBackupDate,
        };
    }
}
