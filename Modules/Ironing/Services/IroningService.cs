using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Ironing.Services;

public class IroningService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : IIroningService
{
    // ── Single entries ──

    public async Task<IReadOnlyList<IroningEntry>> GetAllEntriesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("IroningService.GetAllEntriesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.IroningEntries
            .AsNoTracking()
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateEntryAsync(IroningEntryDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("IroningService.CreateEntryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new IroningEntry
        {
            Date = dto.Date,
            CustomerName = dto.CustomerName.Trim(),
            Items = dto.Items?.Trim() ?? string.Empty,
            Quantity = dto.Quantity,
            Rate = dto.Rate,
            Amount = dto.Amount,
            IsPaid = dto.IsPaid
        };

        context.IroningEntries.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateEntryAsync(int id, IroningEntryDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("IroningService.UpdateEntryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.IroningEntries.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"IroningEntry with Id {id} not found.");

        entity.Date = dto.Date;
        entity.CustomerName = dto.CustomerName.Trim();
        entity.Items = dto.Items?.Trim() ?? string.Empty;
        entity.Quantity = dto.Quantity;
        entity.Rate = dto.Rate;
        entity.Amount = dto.Amount;
        entity.IsPaid = dto.IsPaid;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteEntryAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("IroningService.DeleteEntryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.IroningEntries.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"IroningEntry with Id {id} not found.");

        context.IroningEntries.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task MarkPaidAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("IroningService.MarkPaidAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.IroningEntries.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"IroningEntry with Id {id} not found.");

        entity.IsPaid = true;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Batches ──

    public async Task<IReadOnlyList<IroningBatch>> GetAllBatchesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("IroningService.GetAllBatchesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.IroningBatches
            .Include(b => b.Items)
            .AsNoTracking()
            .OrderByDescending(b => b.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IroningBatch?> GetBatchByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("IroningService.GetBatchByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.IroningBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateBatchAsync(IroningBatchDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("IroningService.CreateBatchAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var batch = new IroningBatch
        {
            Date = dto.Date,
            Note = dto.Note?.Trim() ?? string.Empty,
            Status = "Outward",
            CreatedAt = regional.Now
        };

        foreach (var item in dto.Items)
        {
            batch.Items.Add(new IroningBatchItem
            {
                ClothName = item.ClothName.Trim(),
                Quantity = item.Quantity,
                ReceivedQty = item.ReceivedQty,
                Rate = item.Rate,
                Amount = item.Amount
            });
        }

        context.IroningBatches.Add(batch);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateBatchAsync(int id, IroningBatchDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("IroningService.UpdateBatchAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var batch = await context.IroningBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"IroningBatch with Id {id} not found.");

        batch.Date = dto.Date;
        batch.Note = dto.Note?.Trim() ?? string.Empty;

        context.IroningBatchItems.RemoveRange(batch.Items);

        foreach (var item in dto.Items)
        {
            batch.Items.Add(new IroningBatchItem
            {
                ClothName = item.ClothName.Trim(),
                Quantity = item.Quantity,
                ReceivedQty = item.ReceivedQty,
                Rate = item.Rate,
                Amount = item.Amount
            });
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteBatchAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("IroningService.DeleteBatchAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var batch = await context.IroningBatches.FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"IroningBatch with Id {id} not found.");

        context.IroningBatches.Remove(batch);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SetBatchStatusAsync(int id, string status, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        using var _ = perf.BeginScope("IroningService.SetBatchStatusAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var batch = await context.IroningBatches.FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"IroningBatch with Id {id} not found.");

        batch.Status = status;
        if (status == "Completed")
            batch.CompletedDate = regional.Now;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Cloths master ──

    public async Task<IReadOnlyList<Cloth>> GetClothsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("IroningService.GetClothsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Cloths
            .AsNoTracking()
            .OrderBy(c => c.Category)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateClothAsync(string category, decimal price, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        using var _ = perf.BeginScope("IroningService.CreateClothAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        context.Cloths.Add(new Cloth
        {
            Category = category.Trim(),
            Price = price,
            CreatedDate = regional.Now
        });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteClothAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("IroningService.DeleteClothAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Cloths.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Cloth with Id {id} not found.");

        context.Cloths.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IroningStats> GetStatsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("IroningService.GetStatsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entries = await context.IroningEntries.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        var activeBatches = await context.IroningBatches.CountAsync(b => b.Status != "Completed", ct).ConfigureAwait(false);

        return new IroningStats(
            entries.Count,
            entries.Count(e => !e.IsPaid),
            entries.Sum(e => e.Amount),
            entries.Where(e => e.IsPaid).Sum(e => e.Amount),
            activeBatches);
    }
}
