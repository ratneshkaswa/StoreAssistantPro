using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Branch.Services;

public class BranchBillService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : IBranchBillService
{
    public async Task<IReadOnlyList<BranchBill>> GetAllAsync(string? type = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BranchBillService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var query = context.BranchBills.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(b => b.Type == type);

        return await query
            .OrderByDescending(b => b.Date)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<BranchBill?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BranchBillService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.BranchBills
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(BranchBillDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.BillNo);

        using var _ = perf.BeginScope("BranchBillService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new BranchBill
        {
            Date = dto.Date,
            BillNo = dto.BillNo.Trim(),
            Amount = dto.Amount,
            Type = dto.Type,
            CreatedAt = regional.Now
        };

        context.BranchBills.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, BranchBillDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("BranchBillService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.BranchBills.FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"BranchBill with Id {id} not found.");

        entity.Date = dto.Date;
        entity.BillNo = dto.BillNo.Trim();
        entity.Amount = dto.Amount;
        entity.Type = dto.Type;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BranchBillService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.BranchBills.FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"BranchBill with Id {id} not found.");

        context.BranchBills.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task MarkClearedAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BranchBillService.MarkClearedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.BranchBills.FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"BranchBill with Id {id} not found.");

        entity.IsCleared = true;
        entity.ClearedAt = regional.Now;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<int> ClearAllAsync(int retentionDays = 30, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BranchBillService.ClearAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = regional.Now.AddDays(-retentionDays);
        var cleared = await context.BranchBills
            .Where(b => b.IsCleared && b.ClearedAt.HasValue && b.ClearedAt.Value < cutoff)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        return cleared;
    }

    public async Task<BranchStats> GetStatsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BranchBillService.GetStatsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var all = await context.BranchBills.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        return new BranchStats(
            all.Count,
            all.Count(b => !b.IsCleared),
            all.Sum(b => b.Amount),
            all.Where(b => b.IsCleared).Sum(b => b.Amount));
    }
}
