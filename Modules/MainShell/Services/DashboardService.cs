using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
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
    IPerformanceMonitor perf,
    IReferenceDataCache referenceDataCache) : IDashboardService
{
    private static readonly TimeSpan LiveSummaryTtl = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan HistoricalSummaryTtl = TimeSpan.FromMinutes(3);
    private int _cacheVersion;

    public void InvalidateCache() => System.Threading.Interlocked.Increment(ref _cacheVersion);

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var today = regional.Now.Date;
        return await referenceDataCache.GetOrCreateValueAsync(
            BuildCacheKey("today", today),
            innerCt => BuildCurrentSummaryAsync(today, innerCt),
            LiveSummaryTtl,
            ct).ConfigureAwait(false);
    }

    private async Task<DashboardSummary> BuildCurrentSummaryAsync(DateTime today, CancellationToken ct)
    {
        using var _ = perf.BeginScope("DashboardService.GetSummaryAsync");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var nextDay = today.AddDays(1);
        var yesterday = today.AddDays(-1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var previousMonthStart = monthStart.AddMonths(-1);
        var previousMonthPeriodEnd = previousMonthStart.AddDays((nextDay - monthStart).Days);
        var previousMonthLimit = previousMonthStart.AddMonths(1);
        if (previousMonthPeriodEnd > previousMonthLimit)
            previousMonthPeriodEnd = previousMonthLimit;

        // ── Sales KPIs ──
        var todaySales = await db.Sales
            .Where(s => s.SaleDate >= today)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        var monthSales = await db.Sales
            .Where(s => s.SaleDate >= monthStart)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        var previousDaySales = await db.Sales
            .Where(s => s.SaleDate >= yesterday && s.SaleDate < today)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        var previousMonthSales = await db.Sales
            .Where(s => s.SaleDate >= previousMonthStart && s.SaleDate < previousMonthPeriodEnd)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        // ── Today's returns (#389) ──
        var todayReturns = await db.SaleReturns
            .Where(r => r.ReturnDate >= today)
            .SumAsync(r => r.RefundAmount, ct);

        var previousDayReturns = await db.SaleReturns
            .Where(r => r.ReturnDate >= yesterday && r.ReturnDate < today)
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
        var previousDaySalesTotal = previousDaySales.Sum(s => s.TotalAmount);
        var previousDayNet = previousDaySalesTotal - previousDayReturns;
        var previousDayTxCount = previousDaySales.Count;
        var previousDayAverageBill = previousDayTxCount > 0 ? previousDaySalesTotal / previousDayTxCount : 0;

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

        // ── Top products today (#394) ──
        var topProductsTodayRows = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= today)
            .Select(si => new
            {
                ProductName = si.Product!.Name,
                si.Quantity,
                Revenue = si.UnitPrice * si.Quantity
            })
            .ToListAsync(ct);

        var topProductsToday = topProductsTodayRows
            .GroupBy(si => si.ProductName)
            .Select(g => new TopProductItem(
                g.Key ?? "Unnamed product",
                g.Sum(si => si.Quantity),
                g.Sum(si => si.Revenue)))
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToList();

        // ── Last backup date (#332) ──
        DateTime? lastBackupDate = null;
        try { lastBackupDate = await backupService.GetLastBackupDateAsync(ct); }
        catch { /* non-critical — don't fail dashboard for backup check */ }

        // ── Monthly sales trend (#398) — daily totals for last 30 days ──
        var thirtyDaysAgo = today.AddDays(-29);
        var trendSalesRows = await db.Sales
            .Where(s => s.SaleDate >= thirtyDaysAgo)
            .Select(s => new { s.SaleDate, s.TotalAmount })
            .ToListAsync(ct);

        var trendRaw = trendSalesRows
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new DailySalesTrendItem(g.Key, g.Sum(s => s.TotalAmount), g.Count()))
            .OrderBy(t => t.Date)
            .ToList();

        // Fill missing days with zero
        var trendFilled = new List<DailySalesTrendItem>();
        for (var d = thirtyDaysAgo; d <= today; d = d.AddDays(1))
        {
            var existing = trendRaw.FirstOrDefault(t => t.Date.Date == d);
            trendFilled.Add(existing ?? new DailySalesTrendItem(d, 0, 0));
        }

        // ── Monthly expense trend (#399) ──
        var expenseTrendRows = await db.Expenses
            .Where(e => e.Date >= thirtyDaysAgo)
            .Select(e => new { e.Date, e.Amount })
            .ToListAsync(ct);

        var expenseTrendRaw = expenseTrendRows
            .GroupBy(e => e.Date.Date)
            .Select(g => new DailyExpenseTrendItem(g.Key, g.Sum(e => e.Amount), g.Count()))
            .OrderBy(t => t.Date)
            .ToList();

        var expenseTrendFilled = new List<DailyExpenseTrendItem>();
        for (var d = thirtyDaysAgo; d <= today; d = d.AddDays(1))
        {
            var existing = expenseTrendRaw.FirstOrDefault(t => t.Date.Date == d);
            expenseTrendFilled.Add(existing ?? new DailyExpenseTrendItem(d, 0, 0));
        }

        // ── Category sales breakdown (#400) ──
        var categorySalesRows = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= monthStart)
            .Select(si => new
            {
                CategoryName = si.Product!.Category!.Name,
                si.Quantity,
                Revenue = si.UnitPrice * si.Quantity
            })
            .ToListAsync(ct);

        var categorySales = categorySalesRows
            .GroupBy(si => si.CategoryName ?? "Uncategorized")
            .Select(g => new CategorySalesBreakdownItem(g.Key, g.Sum(si => si.Revenue), g.Sum(si => si.Quantity)))
            .OrderByDescending(c => c.Revenue)
            .Take(10)
            .ToList();

        // ── Year-over-year comparison (#402) ──
        var lastYearMonthStart = monthStart.AddYears(-1);
        var lastYearMonthEnd = lastYearMonthStart.AddMonths(1);
        var sameMonthLastYear = await db.Sales
            .Where(s => s.SaleDate >= lastYearMonthStart && s.SaleDate < lastYearMonthEnd)
            .SumAsync(s => s.TotalAmount, ct);

        // ── Sales target (#403) ──
        var config = await db.AppConfigs.AsNoTracking().FirstOrDefaultAsync(ct);
        var salesTarget = config?.MonthlySalesTarget ?? 0;

        // ── Upcoming tasks (#406) ──
        var pendingPOs = await db.PurchaseOrders
            .CountAsync(po => po.Status == PurchaseOrderStatus.Draft || po.Status == PurchaseOrderStatus.Ordered, ct);

        var overduePayments = await db.Debtors
            .CountAsync(d => d.TotalAmount > d.PaidAmount, ct);

        var backupOverdue = lastBackupDate == null || (today - lastBackupDate.Value.Date).TotalHours > 24;

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
            PreviousDaySales = previousDaySalesTotal,
            PreviousDayReturns = previousDayReturns,
            PreviousDayNetSales = previousDayNet,
            PreviousDayAverageBillValue = previousDayAverageBill,
            PreviousMonthSales = previousMonthSales.Sum(s => s.TotalAmount),
            TotalProducts = totalProducts,
            LowStockCount = lowStock,
            OutOfStockCount = outOfStock,
            PendingOrdersCount = pendingOrders.Count,
            OverdueOrdersCount = overdueOrders,
            OutstandingReceivables = receivables,
            PendingPaymentsCount = pendingPayments,
            RecentSales = recentSales,
            TopProductsToday = topProductsToday,
            DailySalesTrend = trendFilled,
            LastBackupDate = lastBackupDate,
            DailyExpenseTrend = expenseTrendFilled,
            CategorySalesBreakdown = categorySales,
            SameMonthLastYearSales = sameMonthLastYear,
            MonthlySalesTarget = salesTarget,
            PendingPurchaseOrdersCount = pendingPOs,
            OverduePaymentsCount = overduePayments,
            BackupOverdue = backupOverdue,
        };
    }
    public async Task<DashboardSummary> GetSummaryForDateAsync(DateTime date, CancellationToken ct = default)
    {
        var targetDate = date.Date;
        return await referenceDataCache.GetOrCreateValueAsync(
            BuildCacheKey("date", targetDate),
            innerCt => BuildSummaryForDateAsync(targetDate, innerCt),
            ResolveTtl(targetDate),
            ct).ConfigureAwait(false);
    }

    private async Task<DashboardSummary> BuildSummaryForDateAsync(DateTime targetDate, CancellationToken ct)
    {
        using var _ = perf.BeginScope("DashboardService.GetSummaryForDateAsync");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var monthStart = new DateTime(targetDate.Year, targetDate.Month, 1);
        var nextDay = targetDate.AddDays(1);
        var previousDayStart = targetDate.AddDays(-1);
        var previousMonthStart = monthStart.AddMonths(-1);
        var previousMonthPeriodEnd = previousMonthStart.AddDays((nextDay - monthStart).Days);
        var previousMonthLimit = previousMonthStart.AddMonths(1);
        if (previousMonthPeriodEnd > previousMonthLimit)
            previousMonthPeriodEnd = previousMonthLimit;

        // ── Sales KPIs for target date ──
        var daySales = await db.Sales
            .Where(s => s.SaleDate >= targetDate && s.SaleDate < nextDay)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        var monthSales = await db.Sales
            .Where(s => s.SaleDate >= monthStart && s.SaleDate < nextDay)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        var previousDaySales = await db.Sales
            .Where(s => s.SaleDate >= previousDayStart && s.SaleDate < targetDate)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        var previousMonthSales = await db.Sales
            .Where(s => s.SaleDate >= previousMonthStart && s.SaleDate < previousMonthPeriodEnd)
            .Select(s => new { s.TotalAmount })
            .ToListAsync(ct);

        var dayReturns = await db.SaleReturns
            .Where(r => r.ReturnDate >= targetDate && r.ReturnDate < nextDay)
            .SumAsync(r => r.RefundAmount, ct);

        var previousDayReturns = await db.SaleReturns
            .Where(r => r.ReturnDate >= previousDayStart && r.ReturnDate < targetDate)
            .SumAsync(r => r.RefundAmount, ct);

        var dayCost = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= targetDate && si.Sale.SaleDate < nextDay)
            .SumAsync(si => si.Product!.CostPrice * si.Quantity, ct);

        var daySalesTotal = daySales.Sum(s => s.TotalAmount);
        var dayNet = daySalesTotal - dayReturns;
        var dayTxCount = daySales.Count;
        var dayProfit = dayNet - dayCost;
        var avgBill = dayTxCount > 0 ? daySalesTotal / dayTxCount : 0;
        var previousDaySalesTotal = previousDaySales.Sum(s => s.TotalAmount);
        var previousDayNet = previousDaySalesTotal - previousDayReturns;
        var previousDayTxCount = previousDaySales.Count;
        var previousDayAverageBill = previousDayTxCount > 0 ? previousDaySalesTotal / previousDayTxCount : 0;

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

        var topProductsDayRows = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= targetDate && si.Sale.SaleDate < nextDay)
            .Select(si => new
            {
                ProductName = si.Product!.Name,
                si.Quantity,
                Revenue = si.UnitPrice * si.Quantity
            })
            .ToListAsync(ct);

        var topProductsDay = topProductsDayRows
            .GroupBy(si => si.ProductName)
            .Select(g => new TopProductItem(
                g.Key ?? "Unnamed product",
                g.Sum(si => si.Quantity),
                g.Sum(si => si.Revenue)))
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToList();

        DateTime? lastBackupDate = null;
        try { lastBackupDate = await backupService.GetLastBackupDateAsync(ct); }
        catch { /* non-critical */ }

        // ── Monthly sales trend for the target month (#398) ──
        var trendSalesRows = await db.Sales
            .Where(s => s.SaleDate >= monthStart && s.SaleDate < nextDay)
            .Select(s => new { s.SaleDate, s.TotalAmount })
            .ToListAsync(ct);

        var trendRaw = trendSalesRows
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new DailySalesTrendItem(g.Key, g.Sum(s => s.TotalAmount), g.Count()))
            .OrderBy(t => t.Date)
            .ToList();

        var trendFilled = new List<DailySalesTrendItem>();
        for (var d = monthStart; d <= targetDate; d = d.AddDays(1))
        {
            var existing = trendRaw.FirstOrDefault(t => t.Date.Date == d);
            trendFilled.Add(existing ?? new DailySalesTrendItem(d, 0, 0));
        }

        // ── Same month last year (#402) ──
        var lastYearMonthStart = monthStart.AddYears(-1);
        var lastYearMonthEnd = lastYearMonthStart.AddMonths(1);
        var sameMonthLastYear = await db.Sales
            .Where(s => s.SaleDate >= lastYearMonthStart && s.SaleDate < lastYearMonthEnd)
            .SumAsync(s => s.TotalAmount, ct);

        // ── Sales target (#403) ──
        var config = await db.AppConfigs.AsNoTracking().FirstOrDefaultAsync(ct);
        var salesTarget = config?.MonthlySalesTarget ?? 0;

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
            PreviousDaySales = previousDaySalesTotal,
            PreviousDayReturns = previousDayReturns,
            PreviousDayNetSales = previousDayNet,
            PreviousDayAverageBillValue = previousDayAverageBill,
            PreviousMonthSales = previousMonthSales.Sum(s => s.TotalAmount),
            TotalProducts = totalProducts,
            LowStockCount = lowStock,
            OutOfStockCount = outOfStock,
            PendingOrdersCount = pendingOrders.Count,
            OverdueOrdersCount = overdueOrders,
            OutstandingReceivables = receivables,
            PendingPaymentsCount = pendingPayments,
            RecentSales = recentSales,
            TopProductsToday = topProductsDay,
            DailySalesTrend = trendFilled,
            LastBackupDate = lastBackupDate,
            SameMonthLastYearSales = sameMonthLastYear,
            MonthlySalesTarget = salesTarget,
        };
    }

    private string BuildCacheKey(string scope, DateTime date) =>
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"dashboard:{System.Threading.Volatile.Read(ref _cacheVersion)}:{scope}:{date:yyyyMMdd}");

    private TimeSpan ResolveTtl(DateTime date) =>
        date.Date < regional.Now.Date ? HistoricalSummaryTtl : LiveSummaryTtl;
}
