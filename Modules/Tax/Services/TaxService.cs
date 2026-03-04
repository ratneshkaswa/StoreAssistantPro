using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Tax.Services;

public class TaxService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : ITaxService
{
    public async Task<IReadOnlyList<TaxMaster>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.TaxMasters
            .AsNoTracking()
            .OrderBy(t => t.SlabPercent)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TaxMaster>> GetActiveAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxService.GetActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.TaxMasters
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SlabPercent)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(TaxDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto);

        using var _ = perf.BeginScope("TaxService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (await context.TaxMasters.AnyAsync(t => t.TaxName == dto.TaxName.Trim(), ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Tax '{dto.TaxName}' already exists.");

        context.TaxMasters.Add(new TaxMaster
        {
            TaxName = dto.TaxName.Trim(),
            SlabPercent = dto.SlabPercent,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, TaxDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto);

        using var _ = perf.BeginScope("TaxService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaxMasters.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tax with Id {id} not found.");

        if (await context.TaxMasters.AnyAsync(t => t.TaxName == dto.TaxName.Trim() && t.Id != id, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Tax '{dto.TaxName}' already exists.");

        entity.TaxName = dto.TaxName.Trim();
        entity.SlabPercent = dto.SlabPercent;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaxMasters.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tax with Id {id} not found.");

        var inUse = await context.Products.AnyAsync(p => p.TaxId == id, ct).ConfigureAwait(false);
        if (inUse)
            throw new InvalidOperationException("Cannot delete — this tax is assigned to one or more products. Remove the assignment first.");

        context.TaxMasters.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static void ValidateDto(TaxDto dto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.TaxName, nameof(dto.TaxName));

        if (dto.SlabPercent < 0 || dto.SlabPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(dto.SlabPercent), "Slab % must be between 0 and 100.");
    }
}
