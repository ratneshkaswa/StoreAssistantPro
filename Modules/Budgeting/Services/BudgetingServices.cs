using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.Budgeting;

namespace StoreAssistantPro.Modules.Budgeting.Services;

public sealed class BudgetService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<BudgetService> logger) : IBudgetService
{
    public async Task<BudgetEntry> SaveBudgetAsync(BudgetEntry entry, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (entry.Id == 0) context.BudgetEntries.Add(entry); else context.BudgetEntries.Update(entry);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Budget saved: {Type} {Month}/{Year} = ₹{Amount}", entry.BudgetType, entry.Month, entry.Year, entry.BudgetAmount);
        return entry;
    }

    public async Task<IReadOnlyList<BudgetEntry>> GetBudgetsAsync(int year, string? budgetType = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.BudgetEntries.Where(b => b.Year == year);
        if (!string.IsNullOrWhiteSpace(budgetType)) query = query.Where(b => b.BudgetType == budgetType);
        return await query.OrderBy(b => b.Month).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BudgetVariance>> GetVarianceReportAsync(int year, CancellationToken ct = default)
    {
        var budgets = await GetBudgetsAsync(year, ct: ct).ConfigureAwait(false);
        return budgets.Select(b => new BudgetVariance(
            b.BudgetType, b.Category, b.Month, b.Year,
            b.BudgetAmount, b.ActualAmount,
            b.ActualAmount - b.BudgetAmount,
            b.BudgetAmount > 0 ? (double)((b.ActualAmount - b.BudgetAmount) / b.BudgetAmount * 100) : 0
        )).ToList();
    }

    public async Task RefreshActualsAsync(int month, int year, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);
        var salesTotal = await context.Sales.Where(s => s.SaleDate >= from && s.SaleDate < to)
            .SumAsync(s => s.TotalAmount, ct).ConfigureAwait(false);

        var budget = await context.BudgetEntries
            .FirstOrDefaultAsync(b => b.BudgetType == "Sales" && b.Month == month && b.Year == year, ct)
            .ConfigureAwait(false);
        if (budget is not null)
        {
            budget.ActualAmount = salesTotal;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        logger.LogInformation("Refreshed actuals for {Month}/{Year}: ₹{Total}", month, year, salesTotal);
    }
}

public sealed class BudgetForecastService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<BudgetForecastService> logger) : IBudgetForecastService
{
    public async Task<IReadOnlyList<BudgetEntry>> ForecastAsync(int months, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var cutoff = DateTime.UtcNow.AddMonths(-6);
        var avg = await context.Sales.Where(s => s.SaleDate >= cutoff)
            .GroupBy(s => new { s.SaleDate.Month, s.SaleDate.Year })
            .Select(g => g.Sum(s => s.TotalAmount))
            .AverageAsync(ct).ConfigureAwait(false);

        var forecasts = new List<BudgetEntry>();
        var now = DateTime.Today;
        for (int i = 1; i <= months; i++)
        {
            var target = now.AddMonths(i);
            forecasts.Add(new BudgetEntry
            {
                BudgetType = "Sales", Month = target.Month, Year = target.Year,
                BudgetAmount = avg
            });
        }
        logger.LogInformation("Forecast {Months} months at avg ₹{Avg:N0}/month", months, avg);
        return forecasts;
    }
}

public sealed class FinancialGoalService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<FinancialGoalService> logger) : IFinancialGoalService
{
    public async Task<IReadOnlyList<FinancialGoal>> GetGoalsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.FinancialGoals.OrderBy(g => g.TargetDate).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<FinancialGoal> SaveGoalAsync(FinancialGoal goal, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (goal.Id == 0) context.FinancialGoals.Add(goal); else context.FinancialGoals.Update(goal);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return goal;
    }

    public async Task RefreshProgressAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var goals = await context.FinancialGoals.Where(g => !g.IsAchieved).ToListAsync(ct).ConfigureAwait(false);
        foreach (var goal in goals)
        {
            if (goal.MetricType == "Revenue")
            {
                goal.CurrentValue = await context.Sales.SumAsync(s => s.TotalAmount, ct).ConfigureAwait(false);
                goal.IsAchieved = goal.CurrentValue >= goal.TargetValue;
            }
        }
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Refreshed {Count} financial goals", goals.Count);
    }
}
