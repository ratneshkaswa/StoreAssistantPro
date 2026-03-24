using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Paging;
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

    public async Task<PagedResult<Expense>> GetPagedAsync(PagedQuery query, string? search = null, string? dateFilter = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.Expenses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(e => e.Category.Contains(term));
        }

        var today = regional.Now.Date;
        if (!string.IsNullOrWhiteSpace(dateFilter) && dateFilter != "All")
        {
            q = dateFilter switch
            {
                "Today" => q.Where(e => e.Date.Date == today),
                "Week" => q.Where(e => e.Date.Date >= today.AddDays(-(int)today.DayOfWeek)),
                "Month" => q.Where(e => e.Date.Year == today.Year && e.Date.Month == today.Month),
                _ => q
            };
        }

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<Expense>(items, totalCount, query.Page, query.PageSize);
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
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Cash" : dto.PaymentMethod.Trim(),
            Description = dto.Description?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim(),
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
        entity.PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Cash" : dto.PaymentMethod.Trim();
        entity.Description = dto.Description?.Trim();

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

    // ── Expense Categories (#227/#228) ────────────────────────────

    public async Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.GetCategoriesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ExpenseCategories
            .AsNoTracking()
            .Where(ec => ec.IsActive)
            .OrderBy(ec => ec.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateCategoryAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var _ = perf.BeginScope("ExpenseService.CreateCategoryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var exists = await context.ExpenseCategories
            .AnyAsync(ec => ec.Name == name.Trim(), ct)
            .ConfigureAwait(false);
        if (exists) throw new InvalidOperationException($"Expense category '{name}' already exists.");

        context.ExpenseCategories.Add(new ExpenseCategory { Name = name.Trim() });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateCategoryAsync(int id, string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var _ = perf.BeginScope("ExpenseService.UpdateCategoryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.ExpenseCategories.FirstOrDefaultAsync(ec => ec.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"ExpenseCategory with Id {id} not found.");

        if (entity.IsSystem)
            throw new InvalidOperationException("System categories cannot be renamed.");

        entity.Name = name.Trim();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.DeleteCategoryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.ExpenseCategories.FirstOrDefaultAsync(ec => ec.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"ExpenseCategory with Id {id} not found.");

        if (entity.IsSystem)
            throw new InvalidOperationException("System categories cannot be deleted.");

        entity.IsActive = false;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SeedDefaultCategoriesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.SeedDefaultCategoriesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var existing = await context.ExpenseCategories.AnyAsync(ct).ConfigureAwait(false);
        if (existing) return;

        string[] defaults = ["Rent", "Utilities", "Salary", "Transport", "Packaging", "Marketing", "Maintenance", "Misc"];
        foreach (var name in defaults)
        {
            context.ExpenseCategories.Add(new ExpenseCategory { Name = name, IsSystem = true });
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Monthly Expense Report (#232) ─────────────────────────────

    public async Task<MonthlyExpenseReport> GetMonthlyExpenseReportAsync(int year, int month, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.GetMonthlyExpenseReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var expenses = await context.Expenses
            .AsNoTracking()
            .Where(e => e.Date >= monthStart && e.Date < monthEnd)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byCategory = expenses
            .GroupBy(e => e.Category)
            .Select(g => new CategoryExpenseBreakdown(g.Key, g.Sum(e => e.Amount), g.Count()))
            .OrderByDescending(c => c.Amount)
            .ToList();

        return new MonthlyExpenseReport(year, month, expenses.Sum(e => e.Amount), expenses.Count, byCategory);
    }

    // ── Recurring Expenses (#234) ─────────────────────────────────

    public async Task<IReadOnlyList<RecurringExpense>> GetRecurringExpensesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.GetRecurringExpensesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.RecurringExpenses
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Category)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateRecurringExpenseAsync(RecurringExpenseDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Category);

        using var _ = perf.BeginScope("ExpenseService.CreateRecurringExpenseAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        context.RecurringExpenses.Add(new RecurringExpense
        {
            Category = dto.Category.Trim(),
            Description = dto.Description?.Trim(),
            Amount = dto.Amount,
            Frequency = dto.Frequency,
            DueDay = Math.Clamp(dto.DueDay, 1, 28),
            StartDate = dto.StartDate ?? regional.Now.Date,
            EndDate = dto.EndDate,
            CreatedDate = regional.Now
        });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateRecurringExpenseAsync(int id, RecurringExpenseDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("ExpenseService.UpdateRecurringExpenseAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.RecurringExpenses
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"RecurringExpense Id {id} not found.");

        entity.Category = dto.Category.Trim();
        entity.Description = dto.Description?.Trim();
        entity.Amount = dto.Amount;
        entity.Frequency = dto.Frequency;
        entity.DueDay = Math.Clamp(dto.DueDay, 1, 28);
        entity.EndDate = dto.EndDate;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteRecurringExpenseAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.DeleteRecurringExpenseAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.RecurringExpenses
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"RecurringExpense Id {id} not found.");

        entity.IsActive = false;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<int> GenerateDueRecurringExpensesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ExpenseService.GenerateDueRecurringExpensesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var now = regional.Now;
        var templates = await context.RecurringExpenses
            .Where(r => r.IsActive && r.StartDate <= now && (r.EndDate == null || r.EndDate >= now))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var count = 0;
        foreach (var template in templates)
        {
            var dueDate = GetNextDueDate(template, now);
            if (dueDate > now.Date) continue;
            if (template.LastGeneratedDate.HasValue && template.LastGeneratedDate.Value.Date >= dueDate) continue;

            context.Expenses.Add(new Expense
            {
                Date = dueDate,
                Category = template.Category,
                Amount = template.Amount,
                Description = $"[Auto] {template.Description ?? template.Category}",
                CreatedAt = now
            });

            template.LastGeneratedDate = now;
            count++;
        }

        if (count > 0)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return count;
    }

    // ── Expense Approval (#235) ───────────────────────────────────

    public async Task CreateWithApprovalAsync(ExpenseDto dto, bool isApproved = false, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Category);

        using var _ = perf.BeginScope("ExpenseService.CreateWithApprovalAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var threshold = config?.ExpenseApprovalThreshold ?? 0;

        if (threshold > 0 && dto.Amount > threshold && !isApproved)
            throw new InvalidOperationException(
                $"Expenses above {threshold:N0} require manager approval.");

        var entity = new Expense
        {
            Date = dto.Date,
            Category = dto.Category.Trim(),
            Amount = dto.Amount,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Cash" : dto.PaymentMethod.Trim(),
            Description = dto.Description?.Trim(),
            CreatedBy = dto.CreatedBy?.Trim(),
            CreatedAt = regional.Now
        };

        context.Expenses.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Private helpers ───────────────────────────────────────────

    private static DateTime GetNextDueDate(RecurringExpense template, DateTime now)
    {
        return template.Frequency switch
        {
            "Weekly" => template.LastGeneratedDate?.AddDays(7).Date ?? template.StartDate.Date,
            "Quarterly" => template.LastGeneratedDate?.AddMonths(3).Date ?? template.StartDate.Date,
            "Annually" => template.LastGeneratedDate?.AddYears(1).Date ?? template.StartDate.Date,
            _ => new DateTime(now.Year, now.Month, Math.Min(template.DueDay, DateTime.DaysInMonth(now.Year, now.Month)))
        };
    }
}
