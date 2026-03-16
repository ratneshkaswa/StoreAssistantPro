using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Reports.Services;

public class ReportsService(
    IDbContextFactory<AppDbContext> contextFactory,
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
}
