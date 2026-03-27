using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Tax.Services;

public class TaxService(
    IDbContextFactory<AppDbContext> contextFactory,
    IAuditService auditService,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf,
    IReferenceDataCache referenceDataCache) : ITaxService
{
    private static readonly TimeSpan ReferenceDataTtl = TimeSpan.FromMinutes(5);
    private const string ActiveTaxesCacheKey = "TaxMasters.Active";

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
        return await referenceDataCache.GetOrCreateAsync<TaxMaster>(
            ActiveTaxesCacheKey,
            async innerCt =>
            {
                await using var context = await contextFactory.CreateDbContextAsync(innerCt).ConfigureAwait(false);
                return await context.TaxMasters
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.SlabPercent)
                    .ToListAsync(innerCt)
                    .ConfigureAwait(false);
            },
            ReferenceDataTtl,
            ct).ConfigureAwait(false);
    }

    public async Task CreateAsync(TaxDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto);

        using var scope = perf.BeginScope("TaxService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (await context.TaxMasters.AnyAsync(t => t.TaxName == dto.TaxName.Trim(), ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Tax '{dto.TaxName}' already exists.");

        context.TaxMasters.Add(new TaxMaster
        {
            TaxName = dto.TaxName.Trim(),
            SlabPercent = dto.SlabPercent,
            IsActive = true,
            CreatedDate = regional.Now
        });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        referenceDataCache.Invalidate(ActiveTaxesCacheKey);

        // Audit: tax profile created (#203)
        _ = auditService.LogAsync("TaxCreated", "TaxMaster", null,
            null, $"{dto.TaxName.Trim()} @ {dto.SlabPercent}%", null,
            $"Tax profile '{dto.TaxName.Trim()}' created with rate {dto.SlabPercent}%", ct);
    }

    public async Task UpdateAsync(int id, TaxDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto);

        using var scope = perf.BeginScope("TaxService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaxMasters.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tax with Id {id} not found.");

        if (await context.TaxMasters.AnyAsync(t => t.TaxName == dto.TaxName.Trim() && t.Id != id, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Tax '{dto.TaxName}' already exists.");

        // Capture old values for audit (#203)
        var oldName = entity.TaxName;
        var oldRate = entity.SlabPercent;

        entity.TaxName = dto.TaxName.Trim();
        entity.SlabPercent = dto.SlabPercent;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        referenceDataCache.Invalidate(ActiveTaxesCacheKey);

        // Audit: tax rate change (#203)
        if (oldRate != dto.SlabPercent || oldName != dto.TaxName.Trim())
            _ = auditService.LogAsync("TaxUpdated", "TaxMaster", id.ToString(),
                $"{oldName} @ {oldRate}%", $"{dto.TaxName.Trim()} @ {dto.SlabPercent}%", null,
                $"Tax profile updated: {oldName} {oldRate}% → {dto.TaxName.Trim()} {dto.SlabPercent}%", ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var scope = perf.BeginScope("TaxService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaxMasters.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tax with Id {id} not found.");

        var inUse = await context.Products.AnyAsync(p => p.TaxId == id, ct).ConfigureAwait(false);
        if (inUse)
            throw new InvalidOperationException("Cannot delete — this tax is assigned to one or more products. Remove the assignment first.");

        var deletedName = entity.TaxName;
        var deletedRate = entity.SlabPercent;

        context.TaxMasters.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        referenceDataCache.Invalidate(ActiveTaxesCacheKey);

        // Audit: tax profile deleted (#203)
        _ = auditService.LogAsync("TaxDeleted", "TaxMaster", id.ToString(),
            $"{deletedName} @ {deletedRate}%", null, null,
            $"Tax profile '{deletedName}' ({deletedRate}%) deleted", ct);
    }

    private static void ValidateDto(TaxDto dto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.TaxName, nameof(dto.TaxName));

        if (dto.SlabPercent < 0 || dto.SlabPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(dto.SlabPercent), "Slab % must be between 0 and 100.");
    }
}
