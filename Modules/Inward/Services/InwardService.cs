using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inward.Services;

public class InwardService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : IInwardService
{
    public async Task<int> GetNextSequenceAsync(int month, int year, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InwardService.GetNextSequenceAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var prefix = $"{month:D2}-";
        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        var maxSequence = await context.InwardParcels
            .Where(p => p.InwardEntry != null
                && p.InwardEntry.InwardDate >= startOfMonth
                && p.InwardEntry.InwardDate < endOfMonth)
            .CountAsync(ct)
            .ConfigureAwait(false);

        return maxSequence + 1;
    }

    public string FormatParcelNumber(int month, int sequence) =>
        $"{month:D2}-{sequence:D2}";

    public string FormatInwardNumber(int month, int firstSequence) =>
        $"{month:D2}-{firstSequence:D2}";

    public async Task<InwardEntry> SaveInwardEntryAsync(InwardEntry entry, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InwardService.SaveInwardEntryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        entry.CreatedAt = regional.Now;

        if (entry.Id == 0)
        {
            context.InwardEntries.Add(entry);
        }
        else
        {
            context.InwardEntries.Update(entry);
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry;
    }

    public async Task<IReadOnlyList<InwardEntry>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InwardService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.InwardEntries
            .AsNoTracking()
            .Include(e => e.Parcels).ThenInclude(p => p.Category)
            .Include(e => e.Parcels).ThenInclude(p => p.Vendor)
            .OrderByDescending(e => e.InwardDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InwardEntry>> GetByMonthAsync(int month, int year, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InwardService.GetByMonthAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        return await context.InwardEntries
            .AsNoTracking()
            .Include(e => e.Parcels).ThenInclude(p => p.Category)
            .Include(e => e.Parcels).ThenInclude(p => p.Vendor)
            .Where(e => e.InwardDate >= startOfMonth && e.InwardDate < endOfMonth)
            .OrderByDescending(e => e.InwardDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<InwardEntry?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InwardService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.InwardEntries
            .AsNoTracking()
            .Include(e => e.Parcels).ThenInclude(p => p.Category)
            .Include(e => e.Parcels).ThenInclude(p => p.Vendor)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InwardService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entry = await context.InwardEntries
            .Include(e => e.Parcels)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inward entry not found.");

        if (rowVersion is not null)
            context.Entry(entry).Property(e => e.RowVersion).OriginalValue = rowVersion;

        context.InwardEntries.Remove(entry);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
