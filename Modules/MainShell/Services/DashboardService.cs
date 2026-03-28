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
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        var nextDay = today.AddDays(1);
        var yesterday = today.AddDays(-1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var previousMonthStart = monthStart.AddMonths(-1);
        var previousMonthPeriodEnd = previousMonthStart.AddDays((nextDay - monthStart).Days);
        var previousMonthLimit = previousMonthStart.AddMonths(1);
        if (previousMonthPeriodEnd > previousMonthLimit)
            previousMonthPeriodEnd = previousMonthLimit;
        var lastBackupDateTask = GetLastBackupDateSafeAsync(ct);

        // ── Sales KPIs ──
        var todaySalesAggregate = await GetSalesAggregateAsync(
            db.Sales.Where(s => s.SaleDate >= today),
            ct);

        var monthSalesAggregate = await GetSalesAggregateAsync(
            db.Sales.Where(s => s.SaleDate >= monthStart),
            ct);

        var previousDaySalesAggregate = await GetSalesAggregateAsync(
            db.Sales.Where(s => s.SaleDate >= yesterday && s.SaleDate < today),
            ct);

        var previousMonthSalesAggregate = await GetSalesAggregateAsync(
            db.Sales.Where(s => s.SaleDate >= previousMonthStart && s.SaleDate < previousMonthPeriodEnd),
            ct);

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

        var todaySalesTotal = todaySalesAggregate.TotalAmount;
        var todayNet = todaySalesTotal - todayReturns;
        var todayTxCount = todaySalesAggregate.TransactionCount;
        var todayProfit = todayNet - todayCost;
        var averageBill = todayTxCount > 0 ? todaySalesTotal / todayTxCount : 0;
        var previousDaySalesTotal = previousDaySalesAggregate.TotalAmount;
        var previousDayNet = previousDaySalesTotal - previousDayReturns;
        var previousDayTxCount = previousDaySalesAggregate.TransactionCount;
        var previousDayAverageBill = previousDayTxCount > 0 ? previousDaySalesTotal / previousDayTxCount : 0;

        // ── Inventory KPIs ──
        var totalProducts = await db.Products.CountAsync(ct);
        var outOfStock = await db.Products.CountAsync(p => p.Quantity <= 0, ct);
        var lowStock = await db.Products.CountAsync(
            p => p.MinStockLevel > 0 && p.Quantity > 0 && p.Quantity <= p.MinStockLevel,
            ct);

        // ── Orders ──
        var pendingOrdersCount = await db.Orders.CountAsync(o => o.Status == "Pending", ct);
        var overdueOrders = await db.Orders.CountAsync(
            o => o.Status == "Pending"
                && o.DeliveryDate.HasValue
                && o.DeliveryDate.Value.Date < today,
            ct);

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
        var topProductsToday = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= today)
            .GroupBy(si => si.Product!.Name)
            .Select(g => new TopProductItem(
                g.Key ?? "Unnamed product",
                g.Sum(si => si.Quantity),
                g.Sum(si => si.UnitPrice * si.Quantity)))
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToListAsync(ct);

        // ── Last backup date (#332) ──
        var lastBackupDate = await lastBackupDateTask.ConfigureAwait(false);

        // ── Monthly sales trend (#398) — daily totals for last 30 days ──
        var thirtyDaysAgo = today.AddDays(-29);
        var trendSalesRows = await db.Sales
            .Where(s => s.SaleDate >= thirtyDaysAgo)
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalSales = g.Sum(s => s.TotalAmount),
                TransactionCount = g.Count()
            })
            .OrderBy(g => g.Date)
            .ToListAsync(ct);

        var trendRaw = trendSalesRows
            .Select(row => new DailySalesTrendItem(row.Date, row.TotalSales, row.TransactionCount))
            .ToList();
        var trendFilled = FillDailySalesTrend(trendRaw, thirtyDaysAgo, today);

        // ── Monthly expense trend (#399) ──
        var expenseTrendRows = await db.Expenses
            .Where(e => e.Date >= thirtyDaysAgo)
            .GroupBy(e => e.Date.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalExpenses = g.Sum(e => e.Amount),
                ExpenseCount = g.Count()
            })
            .OrderBy(g => g.Date)
            .ToListAsync(ct);

        var expenseTrendRaw = expenseTrendRows
            .Select(row => new DailyExpenseTrendItem(row.Date, row.TotalExpenses, row.ExpenseCount))
            .ToList();
        var expenseTrendFilled = FillDailyExpenseTrend(expenseTrendRaw, thirtyDaysAgo, today);

        // ── Category sales breakdown (#400) ──
        var categorySales = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= monthStart)
            .GroupBy(si => si.Product!.Category!.Name)
            .Select(g => new CategorySalesBreakdownItem(
                g.Key ?? "Uncategorized",
                g.Sum(si => si.UnitPrice * si.Quantity),
                g.Sum(si => si.Quantity)))
            .OrderByDescending(c => c.Revenue)
            .Take(10)
            .ToListAsync(ct);

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
            ThisMonthSales = monthSalesAggregate.TotalAmount,
            ThisMonthTransactions = monthSalesAggregate.TransactionCount,
            PreviousDaySales = previousDaySalesTotal,
            PreviousDayReturns = previousDayReturns,
            PreviousDayNetSales = previousDayNet,
            PreviousDayAverageBillValue = previousDayAverageBill,
            PreviousMonthSales = previousMonthSalesAggregate.TotalAmount,
            TotalProducts = totalProducts,
            LowStockCount = lowStock,
            OutOfStockCount = outOfStock,
            PendingOrdersCount = pendingOrdersCount,
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
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        var monthStart = new DateTime(targetDate.Year, targetDate.Month, 1);
        var nextDay = targetDate.AddDays(1);
        var previousDayStart = targetDate.AddDays(-1);
        var previousMonthStart = monthStart.AddMonths(-1);
        var previousMonthPeriodEnd = previousMonthStart.AddDays((nextDay - monthStart).Days);
        var previousMonthLimit = previousMonthStart.AddMonths(1);
        if (previousMonthPeriodEnd > previousMonthLimit)
            previousMonthPeriodEnd = previousMonthLimit;
        var lastBackupDateTask = GetLastBackupDateSafeAsync(ct);

        // ── Sales KPIs for target date ──
        var daySalesAggregate = await GetSalesAggregateAsync(
            db.Sales.Where(s => s.SaleDate >= targetDate && s.SaleDate < nextDay),
            ct);

        var monthSalesAggregate = await GetSalesAggregateAsync(
            db.Sales.Where(s => s.SaleDate >= monthStart && s.SaleDate < nextDay),
            ct);

        var previousDaySalesAggregate = await GetSalesAggregateAsync(
            db.Sales.Where(s => s.SaleDate >= previousDayStart && s.SaleDate < targetDate),
            ct);

        var previousMonthSalesAggregate = await GetSalesAggregateAsync(
            db.Sales.Where(s => s.SaleDate >= previousMonthStart && s.SaleDate < previousMonthPeriodEnd),
            ct);

        var dayReturns = await db.SaleReturns
            .Where(r => r.ReturnDate >= targetDate && r.ReturnDate < nextDay)
            .SumAsync(r => r.RefundAmount, ct);

        var previousDayReturns = await db.SaleReturns
            .Where(r => r.ReturnDate >= previousDayStart && r.ReturnDate < targetDate)
            .SumAsync(r => r.RefundAmount, ct);

        var dayCost = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= targetDate && si.Sale.SaleDate < nextDay)
            .SumAsync(si => si.Product!.CostPrice * si.Quantity, ct);

        var daySalesTotal = daySalesAggregate.TotalAmount;
        var dayNet = daySalesTotal - dayReturns;
        var dayTxCount = daySalesAggregate.TransactionCount;
        var dayProfit = dayNet - dayCost;
        var avgBill = dayTxCount > 0 ? daySalesTotal / dayTxCount : 0;
        var previousDaySalesTotal = previousDaySalesAggregate.TotalAmount;
        var previousDayNet = previousDaySalesTotal - previousDayReturns;
        var previousDayTxCount = previousDaySalesAggregate.TransactionCount;
        var previousDayAverageBill = previousDayTxCount > 0 ? previousDaySalesTotal / previousDayTxCount : 0;

        // ── Inventory KPIs (always current) ──
        var totalProducts = await db.Products.CountAsync(ct);
        var outOfStock = await db.Products.CountAsync(p => p.Quantity <= 0, ct);
        var lowStock = await db.Products.CountAsync(
            p => p.MinStockLevel > 0 && p.Quantity > 0 && p.Quantity <= p.MinStockLevel,
            ct);

        // ── Orders (current) ──
        var pendingOrdersCount = await db.Orders.CountAsync(o => o.Status == "Pending", ct);
        var overdueOrders = await db.Orders.CountAsync(
            o => o.Status == "Pending"
                && o.DeliveryDate.HasValue
                && o.DeliveryDate.Value.Date < targetDate,
            ct);

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

        var topProductsDay = await db.SaleItems
            .Where(si => si.Sale!.SaleDate >= targetDate && si.Sale.SaleDate < nextDay)
            .GroupBy(si => si.Product!.Name)
            .Select(g => new TopProductItem(
                g.Key ?? "Unnamed product",
                g.Sum(si => si.Quantity),
                g.Sum(si => si.UnitPrice * si.Quantity)))
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToListAsync(ct);
        var lastBackupDate = await lastBackupDateTask.ConfigureAwait(false);

        // ── Monthly sales trend for the target month (#398) ──
        var trendSalesRows = await db.Sales
            .Where(s => s.SaleDate >= monthStart && s.SaleDate < nextDay)
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalSales = g.Sum(s => s.TotalAmount),
                TransactionCount = g.Count()
            })
            .OrderBy(g => g.Date)
            .ToListAsync(ct);

        var trendRaw = trendSalesRows
            .Select(row => new DailySalesTrendItem(row.Date, row.TotalSales, row.TransactionCount))
            .ToList();
        var trendFilled = FillDailySalesTrend(trendRaw, monthStart, targetDate);

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
            ThisMonthSales = monthSalesAggregate.TotalAmount,
            ThisMonthTransactions = monthSalesAggregate.TransactionCount,
            PreviousDaySales = previousDaySalesTotal,
            PreviousDayReturns = previousDayReturns,
            PreviousDayNetSales = previousDayNet,
            PreviousDayAverageBillValue = previousDayAverageBill,
            PreviousMonthSales = previousMonthSalesAggregate.TotalAmount,
            TotalProducts = totalProducts,
            LowStockCount = lowStock,
            OutOfStockCount = outOfStock,
            PendingOrdersCount = pendingOrdersCount,
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

    private async Task<DateTime?> GetLastBackupDateSafeAsync(CancellationToken ct)
    {
        try
        {
            return await backupService.GetLastBackupDateAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<SalesAggregate> GetSalesAggregateAsync(
        IQueryable<Sale> salesQuery,
        CancellationToken ct)
    {
        var aggregate = await salesQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalAmount = g.Sum(s => s.TotalAmount),
                TransactionCount = g.Count()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return aggregate is null
            ? SalesAggregate.Empty
            : new SalesAggregate(aggregate.TotalAmount, aggregate.TransactionCount);
    }

    private static List<DailySalesTrendItem> FillDailySalesTrend(
        IReadOnlyList<DailySalesTrendItem> trendRaw,
        DateTime startDate,
        DateTime endDate)
    {
        var byDate = trendRaw.ToDictionary(item => item.Date.Date);
        var filled = new List<DailySalesTrendItem>((endDate - startDate).Days + 1);

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (byDate.TryGetValue(date, out var existing))
                filled.Add(existing);
            else
                filled.Add(new DailySalesTrendItem(date, 0, 0));
        }

        return filled;
    }

    private static List<DailyExpenseTrendItem> FillDailyExpenseTrend(
        IReadOnlyList<DailyExpenseTrendItem> trendRaw,
        DateTime startDate,
        DateTime endDate)
    {
        var byDate = trendRaw.ToDictionary(item => item.Date.Date);
        var filled = new List<DailyExpenseTrendItem>((endDate - startDate).Days + 1);

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (byDate.TryGetValue(date, out var existing))
                filled.Add(existing);
            else
                filled.Add(new DailyExpenseTrendItem(date, 0, 0));
        }

        return filled;
    }

    private sealed record SalesAggregate(decimal TotalAmount, int TransactionCount)
    {
        public static readonly SalesAggregate Empty = new(0, 0);
    }
}
