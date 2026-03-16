using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Salaries.Services;

public class SalaryService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : ISalaryService
{
    public async Task<IReadOnlyList<Salary>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalaryService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Salaries
            .AsNoTracking()
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Salary?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalaryService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Salaries
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(SalaryDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.EmployeeName);

        using var _ = perf.BeginScope("SalaryService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new Salary
        {
            EmployeeName = dto.EmployeeName.Trim(),
            Month = dto.Month,
            Year = dto.Year,
            Amount = dto.Amount,
            BaseSalary = dto.BaseSalary,
            Advance = dto.Advance,
            PresentDays = dto.PresentDays,
            AbsentDays = dto.AbsentDays,
            HoursWorked = dto.HoursWorked,
            Incentive = dto.Incentive,
            Note = dto.Note?.Trim() ?? string.Empty,
            CreatedAt = regional.Now
        };

        context.Salaries.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, SalaryDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("SalaryService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Salaries.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Salary with Id {id} not found.");

        entity.EmployeeName = dto.EmployeeName.Trim();
        entity.Month = dto.Month;
        entity.Year = dto.Year;
        entity.Amount = dto.Amount;
        entity.BaseSalary = dto.BaseSalary;
        entity.Advance = dto.Advance;
        entity.PresentDays = dto.PresentDays;
        entity.AbsentDays = dto.AbsentDays;
        entity.HoursWorked = dto.HoursWorked;
        entity.Incentive = dto.Incentive;
        entity.Note = dto.Note?.Trim() ?? string.Empty;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalaryService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Salaries.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Salary with Id {id} not found.");

        context.Salaries.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task MarkPaidAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalaryService.MarkPaidAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Salaries.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Salary with Id {id} not found.");

        entity.IsPaid = true;
        entity.PaidDate = regional.Now;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<SalaryStats> GetStatsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalaryService.GetStatsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var all = await context.Salaries.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        return new SalaryStats(
            all.Count,
            all.Count(s => s.IsPaid),
            all.Count(s => !s.IsPaid),
            all.Where(s => s.IsPaid).Sum(s => s.Amount),
            all.Where(s => !s.IsPaid).Sum(s => s.NetPay));
    }
}
