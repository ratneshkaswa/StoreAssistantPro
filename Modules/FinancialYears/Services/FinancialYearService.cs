using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.FinancialYears.Services;

public class FinancialYearService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf,
    ILogger<FinancialYearService> logger) : IFinancialYearService
{
    public async Task<IReadOnlyList<FinancialYear>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("FinancialYearService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.FinancialYears
            .AsNoTracking()
            .OrderByDescending(f => f.StartDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<FinancialYear?> GetCurrentAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("FinancialYearService.GetCurrentAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.FinancialYears
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.IsCurrent, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(DateTime startDate, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("FinancialYearService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var endDate = startDate.AddYears(1).AddDays(-1);
        var name = $"{startDate.Year}–{endDate.Year % 100:D2}";

        if (await context.FinancialYears.AnyAsync(f => f.Name == name, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Financial year '{name}' already exists.");

        var entity = new FinancialYear
        {
            Name = name,
            StartDate = startDate,
            EndDate = endDate,
            IsCurrent = false,
            BillingCounterResetDate = startDate,
            CreatedDate = DateTime.UtcNow
        };

        context.FinancialYears.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Financial year '{Name}' created ({Start} to {End})",
            name, startDate.ToShortDateString(), endDate.ToShortDateString());
    }

    public async Task SetCurrentAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("FinancialYearService.SetCurrentAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var target = await context.FinancialYears.FirstOrDefaultAsync(f => f.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Financial year with Id {id} not found.");

        // Clear existing current
        var existing = await context.FinancialYears
            .Where(f => f.IsCurrent)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var fy in existing)
            fy.IsCurrent = false;

        target.IsCurrent = true;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Active financial year set to '{Name}'", target.Name);
    }

    public async Task EnsureCurrentYearAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("FinancialYearService.EnsureCurrentYearAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var now = regional.Now;

        // Determine current FY start: April 1 of current or previous year
        var fyStart = now.Month >= 4
            ? new DateTime(now.Year, 4, 1)
            : new DateTime(now.Year - 1, 4, 1);

        var fyEnd = fyStart.AddYears(1).AddDays(-1);
        var name = $"{fyStart.Year}–{fyEnd.Year % 100:D2}";

        var existing = await context.FinancialYears
            .FirstOrDefaultAsync(f => f.Name == name, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            // Ensure it's marked current
            if (!existing.IsCurrent)
            {
                var others = await context.FinancialYears
                    .Where(f => f.IsCurrent)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                foreach (var fy in others)
                    fy.IsCurrent = false;

                existing.IsCurrent = true;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                logger.LogInformation("Activated existing financial year '{Name}'", name);
            }
            return;
        }

        // Create new FY and reset billing counters
        var others2 = await context.FinancialYears
            .Where(f => f.IsCurrent)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var fy in others2)
            fy.IsCurrent = false;

        context.FinancialYears.Add(new FinancialYear
        {
            Name = name,
            StartDate = fyStart,
            EndDate = fyEnd,
            IsCurrent = true,
            BillingCounterResetDate = fyStart,
            CreatedDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Created and activated financial year '{Name}' with billing counter reset", name);
    }
}
