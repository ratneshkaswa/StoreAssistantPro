using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.SalesPurchase.Services;

public class SalesPurchaseService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : ISalesPurchaseService
{
    public async Task<IReadOnlyList<SalesPurchaseEntry>> GetAllAsync(string? type = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalesPurchaseService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var query = context.SalesPurchaseEntries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.Type == type);

        return await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<SalesPurchaseEntry?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalesPurchaseService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.SalesPurchaseEntries
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(SalesPurchaseEntryDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("SalesPurchaseService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new SalesPurchaseEntry
        {
            Date = dto.Date,
            Note = dto.Note.Trim(),
            Amount = dto.Amount,
            Type = dto.Type,
            CreatedAt = regional.Now
        };

        context.SalesPurchaseEntries.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, SalesPurchaseEntryDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("SalesPurchaseService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.SalesPurchaseEntries.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"SalesPurchaseEntry with Id {id} not found.");

        entity.Date = dto.Date;
        entity.Note = dto.Note.Trim();
        entity.Amount = dto.Amount;
        entity.Type = dto.Type;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalesPurchaseService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.SalesPurchaseEntries.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"SalesPurchaseEntry with Id {id} not found.");

        context.SalesPurchaseEntries.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<SalesPurchaseStats> GetStatsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalesPurchaseService.GetStatsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var all = await context.SalesPurchaseEntries.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        var sales = all.Where(e => e.Type == "Sales").Sum(e => e.Amount);
        var purchases = all.Where(e => e.Type == "Purchase").Sum(e => e.Amount);

        return new SalesPurchaseStats(sales, purchases, sales - purchases, all.Count);
    }
}
