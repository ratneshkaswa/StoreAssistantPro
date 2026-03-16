using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Expenses.Services;

public class ExpenseService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : IExpenseService
{
    public async Task<IReadOnlyList<Expense>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Expenses
            .AsNoTracking()
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Expense?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(ExpenseDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Category);

        using var _ = perf.BeginScope("ExpenseService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new Expense
        {
            Date = dto.Date,
            Category = dto.Category.Trim(),
            Amount = dto.Amount,
            CreatedAt = regional.Now
        };

        context.Expenses.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, ExpenseDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("ExpenseService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Expense with Id {id} not found.");

        entity.Date = dto.Date;
        entity.Category = dto.Category.Trim();
        entity.Amount = dto.Amount;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Expense with Id {id} not found.");

        context.Expenses.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PettyCashDeposit>> GetDepositsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.GetDepositsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.PettyCashDeposits
            .AsNoTracking()
            .OrderByDescending(d => d.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateDepositAsync(PettyCashDepositDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("ExpenseService.CreateDepositAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new PettyCashDeposit
        {
            Date = dto.Date,
            Amount = dto.Amount,
            Note = dto.Note?.Trim() ?? string.Empty,
            CreatedAt = regional.Now
        };

        context.PettyCashDeposits.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteDepositAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.DeleteDepositAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.PettyCashDeposits.FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"PettyCashDeposit with Id {id} not found.");

        context.PettyCashDeposits.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<ExpenseStats> GetStatsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.GetStatsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var today = regional.Now.Date;
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var expenses = await context.Expenses.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        var totalExpenses = expenses.Sum(e => e.Amount);
        var totalDeposits = await context.PettyCashDeposits.SumAsync(d => d.Amount, ct).ConfigureAwait(false);
        var todaySpent = expenses.Where(e => e.Date.Date == today).Sum(e => e.Amount);
        var thisMonthSpent = expenses.Where(e => e.Date >= thisMonthStart).Sum(e => e.Amount);
        var lastMonthSpent = expenses.Where(e => e.Date >= lastMonthStart && e.Date < thisMonthStart).Sum(e => e.Amount);

        return new ExpenseStats(totalExpenses, totalDeposits, totalDeposits - totalExpenses,
            expenses.Count, todaySpent, thisMonthSpent, lastMonthSpent);
    }

    public async Task<int> ImportBulkAsync(IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.ImportBulkAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var now = regional.Now;
        var count = 0;

        foreach (var row in rows)
        {
            var category = (row.GetValueOrDefault("Category") ?? "").Trim();
            var amountStr = (row.GetValueOrDefault("Amount") ?? "").Trim();
            var dateStr = (row.GetValueOrDefault("Date") ?? "").Trim();

            if (string.IsNullOrWhiteSpace(category) || !decimal.TryParse(amountStr, out var amount) || amount <= 0)
                continue;

            var date = DateTime.TryParse(dateStr, out var d) ? d : now.Date;

            context.Expenses.Add(new Expense
            {
                Date = date,
                Category = category,
                Amount = amount,
                CreatedAt = now
            });
            count++;
        }

        if (count > 0)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return count;
    }
}
