using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Expenses.Services;

namespace StoreAssistantPro.Modules.Reports.Services;

public class ReportsService(
    IDbContextFactory<AppDbContext> contextFactory,
    IExpenseService expenseService,
    IPerformanceMonitor perf) : IReportsService
{
    public async Task<ExpenseReport> GetExpenseReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetExpenseReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var expenses = await context.Expenses
            .AsNoTracking()
            .Where(e => e.Date >= from && e.Date <= to)
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byCategory = expenses
            .GroupBy(e => e.Category)
            .Select(g => new CategoryBreakdown(g.Key, g.Sum(e => e.Amount)))
            .OrderByDescending(c => c.Amount)
            .ToList();

        var monthlyTrend = expenses
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyTotal($"{g.Key.Year}-{g.Key.Month:D2}", g.Sum(e => e.Amount)))
            .ToList();

        var recent = expenses.Take(20).ToList();

        return new ExpenseReport(expenses.Count, expenses.Sum(e => e.Amount), byCategory, monthlyTrend, recent);
    }

    public async Task<IroningReport> GetIroningReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetIroningReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entries = await context.IroningEntries
            .AsNoTracking()
            .Where(e => e.Date >= from && e.Date <= to)
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new IroningReport(
            entries.Count,
            entries.Sum(e => e.Amount),
            entries.Where(e => e.IsPaid).Sum(e => e.Amount),
            entries.Where(e => !e.IsPaid).Sum(e => e.Amount),
            entries.Take(20).ToList());
    }

    public async Task<OrderReport> GetOrderReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetOrderReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var orders = await context.Orders
            .AsNoTracking()
            .Where(o => o.Date >= from && o.Date <= to)
            .OrderByDescending(o => o.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new OrderReport(
            orders.Count,
            orders.Sum(o => o.Amount),
            orders.Count(o => o.Status == "Delivered"),
            orders.Count(o => o.Status == "Pending"),
            orders.Take(20).ToList());
    }

    public async Task<InwardReport> GetInwardReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetInwardReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entries = await context.InwardEntries
            .Include(e => e.Vendor)
            .AsNoTracking()
            .Where(e => e.InwardDate >= from && e.InwardDate <= to)
            .OrderByDescending(e => e.InwardDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new InwardReport(entries.Count, entries.Sum(e => e.TransportCharges), entries.Take(20).ToList());
    }

    public async Task<DebtorReport> GetDebtorReportAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetDebtorReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var debtors = await context.Debtors.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        var pending = debtors.Where(d => d.Balance > 0).ToList();

        var topDebtors = pending
            .OrderByDescending(d => d.Balance)
            .Take(10)
            .Select(d => new TopDebtor(d.Name, d.Balance))
            .ToList();

        return new DebtorReport(pending.Count, pending.Sum(d => d.Balance), topDebtors);
    }

    // ── Sales & Tax Reports ──────────────────────────────────────

    public async Task<DailySalesSummary> GetDailySalesSummaryAsync(DateTime date, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetDailySalesSummaryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var sales = await context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= dayStart && s.SaleDate < dayEnd)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var returns = await context.SaleReturns
            .AsNoTracking()
            .Where(r => r.ReturnDate >= dayStart && r.ReturnDate < dayEnd)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var totalSales = sales.Sum(s => s.TotalAmount);
        var totalReturns = returns.Sum(r => r.RefundAmount);
        var totalDiscount = sales.Sum(s => s.DiscountAmount);

        // Sum tax from sale items
        var saleIds = sales.Select(s => s.Id).ToList();
        var totalTax = saleIds.Count > 0
            ? await context.SaleItems
                .Where(si => saleIds.Contains(si.SaleId))
                .SumAsync(si => si.TaxAmount, ct)
                .ConfigureAwait(false)
            : 0;

        return new DailySalesSummary(date, sales.Count, totalSales, totalReturns,
            totalSales - totalReturns, totalTax, totalDiscount);
    }

    public async Task<MonthlySalesSummary> GetMonthlySalesSummaryAsync(int year, int month, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetMonthlySalesSummaryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var sales = await context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= monthStart && s.SaleDate < monthEnd)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var saleIds = sales.Select(s => s.Id).ToList();
        var saleItems = saleIds.Count > 0
            ? await context.SaleItems
                .AsNoTracking()
                .Include(si => si.Product)
                .Where(si => saleIds.Contains(si.SaleId))
                .ToListAsync(ct)
                .ConfigureAwait(false)
            : [];

        var revenue = sales.Sum(s => s.TotalAmount);
        var cogs = saleItems.Sum(si => si.Quantity * (si.Product?.CostPrice ?? 0));
        var totalTax = saleItems.Sum(si => si.TaxAmount);
        var totalDiscount = sales.Sum(s => s.DiscountAmount);

        return new MonthlySalesSummary(year, month, sales.Count, revenue, cogs,
            revenue - cogs, totalTax, totalDiscount);
    }

    public async Task<IReadOnlyList<HsnTaxSummaryLine>> GetHsnTaxSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetHsnTaxSummaryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var items = await context.SaleItems
            .AsNoTracking()
            .Include(si => si.Product)
            .Include(si => si.Sale)
            .Where(si => si.Sale!.SaleDate >= from && si.Sale!.SaleDate <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return items
            .GroupBy(si => si.Product?.HSNCode ?? "N/A")
            .Select(g => new HsnTaxSummaryLine(
                g.Key,
                g.Sum(si => si.Subtotal),
                g.Sum(si => si.CgstAmount),
                g.Sum(si => si.SgstAmount),
                0, // IGST — inter-state not tracked yet
                g.Sum(si => si.TaxAmount),
                g.First().CgstRate,
                g.First().SgstRate))
            .OrderBy(h => h.HsnCode)
            .ToList();
    }

    public async Task<TaxReport> GetTaxReportAsync(int year, int month, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetTaxReportAsync");
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1).AddTicks(-1);

        var hsnLines = await GetHsnTaxSummaryAsync(from, to, ct);

        return new TaxReport(
            year, month,
            hsnLines.Sum(h => h.TotalTax),
            hsnLines.Sum(h => h.CgstAmount),
            hsnLines.Sum(h => h.SgstAmount),
            0, // IGST
            hsnLines);
    }

    public async Task<DailyDiscountReport> GetDailyDiscountReportAsync(DateTime date, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetDailyDiscountReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var discountedSales = await context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= dayStart && s.SaleDate < dayEnd && s.DiscountAmount > 0)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var totalDiscount = discountedSales.Sum(s => s.DiscountAmount);
        var totalBefore = discountedSales.Sum(s => s.TotalAmount + s.DiscountAmount);
        var avgPct = totalBefore > 0 ? totalDiscount / totalBefore * 100 : 0;

        return new DailyDiscountReport(date, discountedSales.Count, totalDiscount, avgPct);
    }

    public async Task<IReadOnlyList<DiscountHistoryEntry>> GetDiscountHistoryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetDiscountHistoryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sales = await context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= from && s.SaleDate <= to && s.DiscountAmount > 0)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return sales.Select(s => new DiscountHistoryEntry(
            s.Id,
            s.InvoiceNumber,
            s.SaleDate,
            s.DiscountType.ToString(),
            s.DiscountValue,
            s.DiscountAmount,
            s.DiscountReason,
            s.CashierRole)).ToList();
    }

    public async Task<IReadOnlyList<ProductSalesSummary>> GetProductSalesReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetProductSalesReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var items = await context.SaleItems
            .AsNoTracking()
            .Include(si => si.Product)
            .Include(si => si.Sale)
            .Where(si => si.Sale!.SaleDate >= from && si.Sale!.SaleDate <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return items
            .GroupBy(si => si.ProductId)
            .Select(g =>
            {
                var first = g.First();
                var totalQty = g.Sum(si => si.Quantity);
                var totalRevenue = g.Sum(si => si.Subtotal);
                var totalTax = g.Sum(si => si.TaxAmount);
                var totalDiscount = g.Sum(si => si.ItemDiscountAmount * si.Quantity + si.ItemFlatDiscount * si.Quantity);
                return new ProductSalesSummary(
                    first.ProductId,
                    first.Product?.Name ?? $"#{first.ProductId}",
                    first.Product?.HSNCode,
                    totalQty,
                    totalRevenue,
                    totalTax,
                    totalDiscount,
                    totalQty > 0 ? totalRevenue / totalQty : 0);
            })
            .OrderByDescending(p => p.TotalRevenue)
            .ToList();
    }

    // ── Category-wise sales (#258) ────────────────────────────────

    public async Task<IReadOnlyList<CategorySalesSummary>> GetCategorySalesReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetCategorySalesReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var items = await context.SaleItems
            .AsNoTracking()
            .Include(si => si.Product).ThenInclude(p => p!.Category)
            .Include(si => si.Sale)
            .Where(si => si.Sale!.SaleDate >= from && si.Sale!.SaleDate <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return items
            .GroupBy(si => new { si.Product?.CategoryId, Name = si.Product?.Category?.Name ?? "Uncategorized" })
            .Select(g =>
            {
                var totalRevenue = g.Sum(si => si.Subtotal);
                var totalCost = g.Sum(si => si.Quantity * (si.Product?.CostPrice ?? 0));
                var grossProfit = totalRevenue - totalCost;
                return new CategorySalesSummary(
                    g.Key.CategoryId,
                    g.Key.Name,
                    g.Select(si => si.ProductId).Distinct().Count(),
                    g.Sum(si => si.Quantity),
                    totalRevenue,
                    totalCost,
                    grossProfit,
                    totalRevenue > 0 ? Math.Round(grossProfit / totalRevenue * 100, 1) : 0);
            })
            .OrderByDescending(c => c.TotalRevenue)
            .ToList();
    }

    // ── Brand-wise sales (#259) ───────────────────────────────────

    public async Task<IReadOnlyList<BrandSalesSummary>> GetBrandSalesReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetBrandSalesReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var items = await context.SaleItems
            .AsNoTracking()
            .Include(si => si.Product).ThenInclude(p => p!.Brand)
            .Include(si => si.Sale)
            .Where(si => si.Sale!.SaleDate >= from && si.Sale!.SaleDate <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return items
            .GroupBy(si => new { si.Product?.BrandId, Name = si.Product?.Brand?.Name ?? "No Brand" })
            .Select(g =>
            {
                var totalRevenue = g.Sum(si => si.Subtotal);
                var totalCost = g.Sum(si => si.Quantity * (si.Product?.CostPrice ?? 0));
                var grossProfit = totalRevenue - totalCost;
                return new BrandSalesSummary(
                    g.Key.BrandId,
                    g.Key.Name,
                    g.Select(si => si.ProductId).Distinct().Count(),
                    g.Sum(si => si.Quantity),
                    totalRevenue,
                    totalCost,
                    grossProfit,
                    totalRevenue > 0 ? Math.Round(grossProfit / totalRevenue * 100, 1) : 0);
            })
            .OrderByDescending(b => b.TotalRevenue)
            .ToList();
    }

    // ── Gross profit (#261) ───────────────────────────────────────

    public async Task<GrossProfitReport> GetGrossProfitReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetGrossProfitReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sales = await context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var saleIds = sales.Select(s => s.Id).ToList();
        var saleItems = saleIds.Count > 0
            ? await context.SaleItems
                .AsNoTracking()
                .Include(si => si.Product)
                .Where(si => saleIds.Contains(si.SaleId))
                .ToListAsync(ct)
                .ConfigureAwait(false)
            : [];

        var totalRevenue = sales.Sum(s => s.TotalAmount);
        var cogs = saleItems.Sum(si => si.Quantity * (si.Product?.CostPrice ?? 0));
        var grossProfit = totalRevenue - cogs;
        var itemsSold = saleItems.Sum(si => si.Quantity);

        return new GrossProfitReport(
            from, to, totalRevenue, cogs, grossProfit,
            totalRevenue > 0 ? Math.Round(grossProfit / totalRevenue * 100, 1) : 0,
            sales.Count, itemsSold);
    }

    // ── Net profit (#262) ─────────────────────────────────────────

    public async Task<NetProfitReport> GetNetProfitReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetNetProfitReportAsync");

        var grossReport = await GetGrossProfitReportAsync(from, to, ct);

        var expenseStats = await expenseService.GetMonthlyExpenseReportAsync(from.Year, from.Month, ct);
        var totalExpenses = expenseStats.TotalAmount;

        // For multi-month ranges, sum all months
        if (from.Month != to.Month || from.Year != to.Year)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            totalExpenses = await context.Expenses
                .AsNoTracking()
                .Where(e => e.Date >= from && e.Date <= to)
                .SumAsync(e => e.Amount, ct)
                .ConfigureAwait(false);
        }

        var netProfit = grossReport.GrossProfit - totalExpenses;

        return new NetProfitReport(
            from, to,
            grossReport.TotalRevenue,
            grossReport.TotalCostOfGoodsSold,
            grossReport.GrossProfit,
            totalExpenses,
            netProfit,
            grossReport.TotalRevenue > 0 ? Math.Round(netProfit / grossReport.TotalRevenue * 100, 1) : 0);
    }

    // ── Best selling products (#263) ──────────────────────────────

    public async Task<IReadOnlyList<ProductSalesSummary>> GetBestSellingProductsAsync(
        DateTime from, DateTime to, int topN = 10, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetBestSellingProductsAsync");
        var all = await GetProductSalesReportAsync(from, to, ct);
        return all.OrderByDescending(p => p.TotalQuantitySold).Take(topN).ToList();
    }

    // ── Slow moving products (#264) ───────────────────────────────

    public async Task<IReadOnlyList<ProductSalesSummary>> GetSlowMovingProductsAsync(
        DateTime from, DateTime to, int topN = 10, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetSlowMovingProductsAsync");
        var all = await GetProductSalesReportAsync(from, to, ct);
        return all.OrderBy(p => p.TotalQuantitySold).Take(topN).ToList();
    }

    // ── Dead stock (#80) ────────────────────────────────────────────

    public async Task<IReadOnlyList<DeadStockItem>> GetDeadStockReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetDeadStockReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var soldProductIds = await context.SaleItems
            .Where(si => si.Sale!.SaleDate >= from && si.Sale.SaleDate <= to)
            .Select(si => si.ProductId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var deadStock = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && p.Quantity > 0 && !soldProductIds.Contains(p.Id))
            .OrderByDescending(p => p.Quantity * p.CostPrice)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return deadStock.Select(p => new DeadStockItem(
            p.Id, p.Name, p.Category?.Name, p.Brand?.Name,
            p.Quantity, p.CostPrice, p.SalePrice,
            p.Quantity * p.CostPrice)).ToList();
    }

    // ── Sales by user (#265) ─────────────────────────────────────

    public async Task<IReadOnlyList<UserSalesSummary>> GetSalesByUserReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetSalesByUserReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sales = await context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return sales
            .GroupBy(s => s.CashierRole ?? "Unknown")
            .Select(g => new UserSalesSummary(
                g.Key,
                g.Count(),
                g.Sum(s => s.TotalAmount),
                g.Sum(s => s.DiscountAmount),
                g.Count() > 0 ? Math.Round(g.Sum(s => s.TotalAmount) / g.Count(), 2) : 0))
            .OrderByDescending(u => u.TotalRevenue)
            .ToList();
    }

    // ── Sales by payment method (#266) ────────────────────────────

    public async Task<IReadOnlyList<PaymentMethodSummary>> GetSalesByPaymentMethodAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetSalesByPaymentMethodAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sales = await context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var grandTotal = sales.Sum(s => s.TotalAmount);

        return sales
            .GroupBy(s => s.PaymentMethod)
            .Select(g => new PaymentMethodSummary(
                g.Key,
                g.Count(),
                g.Sum(s => s.TotalAmount),
                grandTotal > 0 ? Math.Round(g.Sum(s => s.TotalAmount) / grandTotal * 100, 1) : 0))
            .OrderByDescending(p => p.TotalAmount)
            .ToList();
    }

    // ── Profit margin per product (#260) ──────────────────────────

    public async Task<IReadOnlyList<ProductMarginSummary>> GetProductMarginReportAsync(
        CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetProductMarginReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var products = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return products
            .Select(p => new ProductMarginSummary(
                p.Id,
                p.Name,
                p.Category?.Name,
                p.Brand?.Name,
                p.SalePrice,
                p.CostPrice,
                p.Margin,
                p.MarginPercent,
                p.Quantity))
            .OrderByDescending(m => m.MarginPercent)
            .ToList();
    }

    // ── Customer-wise sales (#268) ───────────────────────────────

    public async Task<IReadOnlyList<CustomerSalesSummary>> GetCustomerSalesReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetCustomerSalesReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sales = await context.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Where(s => s.SaleDate >= from && s.SaleDate <= to && s.CustomerId != null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return sales
            .GroupBy(s => new { s.CustomerId, Name = s.Customer!.Name, s.Customer.Phone })
            .Select(g => new CustomerSalesSummary(
                g.Key.CustomerId!.Value,
                g.Key.Name,
                g.Key.Phone,
                g.Count(),
                g.Sum(s => s.TotalAmount),
                g.Sum(s => s.DiscountAmount),
                g.Average(s => s.TotalAmount)))
            .OrderByDescending(c => c.TotalRevenue)
            .ToList();
    }

    // ── Hourly sales distribution (#267) ─────────────────────────

    public async Task<IReadOnlyList<HourlySalesSummary>> GetSalesByHourAsync(
        DateTime date, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetSalesByHourAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var sales = await context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= dayStart && s.SaleDate < dayEnd)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return Enumerable.Range(0, 24)
            .Select(h =>
            {
                var hourSales = sales.Where(s => s.SaleDate.Hour == h).ToList();
                return new HourlySalesSummary(
                    h,
                    hourSales.Count,
                    hourSales.Sum(s => s.TotalAmount),
                    hourSales.Count > 0 ? hourSales.Average(s => s.TotalAmount) : 0);
            })
            .ToList();
    }

    // ── Daily return summary (#151) ──────────────────────────────

    public async Task<DailyReturnSummary> GetDailyReturnSummaryAsync(
        DateTime date, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetDailyReturnSummaryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var returns = await context.SaleReturns
            .AsNoTracking()
            .Where(r => r.ReturnDate >= dayStart && r.ReturnDate < dayEnd)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new DailyReturnSummary(
            date.Date,
            returns.Count,
            returns.Sum(r => r.RefundAmount),
            returns.Sum(r => r.Quantity));
    }

    public async Task<DashboardPrintData> GetDashboardPrintDataAsync(
        DateTime date, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReportsService.GetDashboardPrintDataAsync");

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);
        var dayEndInclusive = dayEnd.AddTicks(-1);

        var dailySales = await GetDailySalesSummaryAsync(dayStart, ct).ConfigureAwait(false);
        var dailyReturns = await GetDailyReturnSummaryAsync(dayStart, ct).ConfigureAwait(false);
        var topProducts = await GetBestSellingProductsAsync(dayStart, dayEndInclusive, 5, ct).ConfigureAwait(false);
        var paymentBreakdown = await GetSalesByPaymentMethodAsync(dayStart, dayEndInclusive, ct).ConfigureAwait(false);

        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var totalExpenses = await context.Expenses
            .AsNoTracking()
            .Where(e => e.Date >= dayStart && e.Date < dayEnd)
            .SumAsync(e => e.Amount, ct)
            .ConfigureAwait(false);

        var saleItems = await context.SaleItems
            .AsNoTracking()
            .Include(si => si.Product)
            .Include(si => si.Sale)
            .Where(si => si.Sale != null && si.Sale.SaleDate >= dayStart && si.Sale.SaleDate < dayEnd)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var costOfGoodsSold = saleItems.Sum(si => si.Quantity * (si.Product?.CostPrice ?? 0));
        var grossProfit = dailySales.NetSales - costOfGoodsSold;
        var averageBillValue = dailySales.SaleCount > 0
            ? Math.Round(dailySales.TotalSales / dailySales.SaleCount, 2)
            : 0;

        return new DashboardPrintData(
            dayStart,
            dailySales.TotalSales,
            dailyReturns.TotalRefundAmount,
            dailySales.NetSales,
            dailySales.SaleCount,
            averageBillValue,
            totalExpenses,
            grossProfit,
            topProducts,
            paymentBreakdown);
    }
}
