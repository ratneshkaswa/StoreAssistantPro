using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Tax.Services;

public class TaxGroupService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : ITaxGroupService
{
    // ── Tax Groups ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<TaxGroup>> GetAllGroupsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.GetAllGroupsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.TaxGroups
            .AsNoTracking()
            .Include(g => g.Slabs.OrderBy(s => s.PriceFrom))
            .OrderBy(g => g.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TaxGroup>> GetActiveGroupsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.GetActiveGroupsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.TaxGroups
            .AsNoTracking()
            .Where(g => g.IsActive)
            .Include(g => g.Slabs.Where(s => s.IsActive).OrderBy(s => s.PriceFrom))
            .OrderBy(g => g.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<TaxGroup?> GetGroupByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.GetGroupByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.TaxGroups
            .AsNoTracking()
            .Include(g => g.Slabs.OrderBy(s => s.PriceFrom))
            .FirstOrDefaultAsync(g => g.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateGroupAsync(TaxGroupDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name, nameof(dto.Name));

        using var _ = perf.BeginScope("TaxGroupService.CreateGroupAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (await context.TaxGroups.AnyAsync(g => g.Name == dto.Name.Trim(), ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Tax group '{dto.Name}' already exists.");

        context.TaxGroups.Add(new TaxGroup
        {
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                IsActive = true,
                    CreatedDate = regional.Now
                });

                await context.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            public async Task UpdateGroupAsync(int id, TaxGroupDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name, nameof(dto.Name));

        using var _ = perf.BeginScope("TaxGroupService.UpdateGroupAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaxGroups.FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tax group Id {id} not found.");

        if (await context.TaxGroups.AnyAsync(g => g.Name == dto.Name.Trim() && g.Id != id, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Tax group '{dto.Name}' already exists.");

        entity.Name = dto.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleGroupActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.ToggleGroupActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaxGroups.FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tax group Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Tax Slabs ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TaxSlab>> GetSlabsByGroupAsync(int taxGroupId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.GetSlabsByGroupAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.TaxSlabs
            .AsNoTracking()
            .Where(s => s.TaxGroupId == taxGroupId)
            .OrderBy(s => s.PriceFrom)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateSlabAsync(TaxSlabDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateSlabDto(dto);

        using var _ = perf.BeginScope("TaxGroupService.CreateSlabAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (!await context.TaxGroups.AnyAsync(g => g.Id == dto.TaxGroupId, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Tax group Id {dto.TaxGroupId} not found.");

        var slab = BuildSlab(dto);
        context.TaxSlabs.Add(slab);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateSlabAsync(int id, TaxSlabDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateSlabDto(dto);

        using var _ = perf.BeginScope("TaxGroupService.UpdateSlabAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaxSlabs.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tax slab Id {id} not found.");

        entity.GSTPercent = dto.GSTPercent;
        entity.CGSTPercent = dto.GSTPercent / 2m;
        entity.SGSTPercent = dto.GSTPercent / 2m;
        entity.IGSTPercent = dto.GSTPercent;
        entity.PriceFrom = dto.PriceFrom;
        entity.PriceTo = dto.PriceTo;
        entity.EffectiveFrom = dto.EffectiveFrom;
        entity.EffectiveTo = dto.EffectiveTo;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteSlabAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.DeleteSlabAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaxSlabs.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tax slab Id {id} not found.");

        context.TaxSlabs.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── HSN Codes ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<HSNCode>> GetAllHSNCodesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.GetAllHSNCodesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.HSNCodes
            .AsNoTracking()
            .OrderBy(h => h.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HSNCode>> GetActiveHSNCodesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.GetActiveHSNCodesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.HSNCodes
            .AsNoTracking()
            .Where(h => h.IsActive)
            .OrderBy(h => h.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateHSNCodeAsync(HSNCodeDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateHSNDto(dto);

        using var _ = perf.BeginScope("TaxGroupService.CreateHSNCodeAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (await context.HSNCodes.AnyAsync(h => h.Code == dto.Code.Trim(), ct).ConfigureAwait(false))
            throw new InvalidOperationException($"HSN code '{dto.Code}' already exists.");

        context.HSNCodes.Add(new HSNCode
        {
            Code = dto.Code.Trim(),
            Description = dto.Description.Trim(),
            Category = dto.Category,
            IsActive = true,
            CreatedDate = regional.Now
        });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateHSNCodeAsync(int id, HSNCodeDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateHSNDto(dto);

        using var _ = perf.BeginScope("TaxGroupService.UpdateHSNCodeAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.HSNCodes.FirstOrDefaultAsync(h => h.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"HSN code Id {id} not found.");

        if (await context.HSNCodes.AnyAsync(h => h.Code == dto.Code.Trim() && h.Id != id, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"HSN code '{dto.Code}' already exists.");

        entity.Code = dto.Code.Trim();
        entity.Description = dto.Description.Trim();
        entity.Category = dto.Category;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleHSNActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.ToggleHSNActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.HSNCodes.FirstOrDefaultAsync(h => h.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"HSN code Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Product Tax Mapping ──────────────────────────────────────────

    public async Task<ProductTaxMapping?> GetMappingByProductAsync(int productId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.GetMappingByProductAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ProductTaxMappings
            .AsNoTracking()
            .Include(m => m.TaxGroup)
            .Include(m => m.HSNCode)
            .FirstOrDefaultAsync(m => m.ProductId == productId, ct)
            .ConfigureAwait(false);
    }

    public async Task SetProductMappingAsync(ProductTaxMappingDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("TaxGroupService.SetProductMappingAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (!await context.Products.AnyAsync(p => p.Id == dto.ProductId, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Product Id {dto.ProductId} not found.");
        if (!await context.TaxGroups.AnyAsync(g => g.Id == dto.TaxGroupId, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Tax group Id {dto.TaxGroupId} not found.");
        if (!await context.HSNCodes.AnyAsync(h => h.Id == dto.HSNCodeId, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"HSN code Id {dto.HSNCodeId} not found.");

        var existing = await context.ProductTaxMappings
            .FirstOrDefaultAsync(m => m.ProductId == dto.ProductId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.TaxGroupId = dto.TaxGroupId;
            existing.HSNCodeId = dto.HSNCodeId;
            existing.OverrideAllowed = dto.OverrideAllowed;
        }
        else
        {
            context.ProductTaxMappings.Add(new ProductTaxMapping
            {
                ProductId = dto.ProductId,
                TaxGroupId = dto.TaxGroupId,
                HSNCodeId = dto.HSNCodeId,
                OverrideAllowed = dto.OverrideAllowed,
                CreatedDate = regional.Now
            });
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveProductMappingAsync(int productId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.RemoveProductMappingAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var mapping = await context.ProductTaxMappings
            .FirstOrDefaultAsync(m => m.ProductId == productId, ct)
            .ConfigureAwait(false);

        if (mapping is not null)
        {
            context.ProductTaxMappings.Remove(mapping);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    // ── Tax Resolution ───────────────────────────────────────────────

    public async Task<TaxSlab?> ResolveSlabAsync(int productId, decimal unitPrice, DateTime date, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.ResolveSlabAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var mapping = await context.ProductTaxMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ProductId == productId, ct)
            .ConfigureAwait(false);

        if (mapping is null)
            return null;

        return await FindEffectiveSlabAsync(context, mapping.TaxGroupId, unitPrice, date, ct)
            .ConfigureAwait(false);
    }

    public async Task<TaxResult> CalculateForProductAsync(
        int productId, decimal unitPrice, decimal quantity,
        bool isIntraState, bool isTaxInclusive, DateTime date,
        CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxGroupService.CalculateForProductAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var mapping = await context.ProductTaxMappings
            .AsNoTracking()
            .Include(m => m.TaxGroup)
            .Include(m => m.HSNCode)
            .FirstOrDefaultAsync(m => m.ProductId == productId, ct)
            .ConfigureAwait(false);

        if (mapping is null)
        {
            var total = decimal.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero);
            return new TaxResult(total, 0, 0, 0, 0, 0, total, null, null);
        }

        var slab = await FindEffectiveSlabAsync(context, mapping.TaxGroupId, unitPrice, date, ct)
            .ConfigureAwait(false);

        if (slab is null)
        {
            var total = decimal.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero);
            return new TaxResult(total, 0, 0, 0, 0, 0, total, mapping.HSNCode?.Code, mapping.TaxGroup?.Name);
        }

        var lineTotal = unitPrice * quantity;
        decimal baseAmount;

        if (isTaxInclusive)
        {
            // Back-calculate: base = total / (1 + rate/100)
            baseAmount = decimal.Round(lineTotal / (1m + slab.GSTPercent / 100m), 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            baseAmount = decimal.Round(lineTotal, 2, MidpointRounding.AwayFromZero);
        }

        decimal cgst, sgst, igst;

        if (isIntraState)
        {
            cgst = decimal.Round(baseAmount * slab.CGSTPercent / 100m, 2, MidpointRounding.AwayFromZero);
            sgst = decimal.Round(baseAmount * slab.SGSTPercent / 100m, 2, MidpointRounding.AwayFromZero);
            igst = 0m;
        }
        else
        {
            cgst = 0m;
            sgst = 0m;
            igst = decimal.Round(baseAmount * slab.IGSTPercent / 100m, 2, MidpointRounding.AwayFromZero);
        }

        var totalTax = cgst + sgst + igst;
        var totalAmount = baseAmount + totalTax;

        return new TaxResult(
            baseAmount, slab.GSTPercent,
            cgst, sgst, igst, totalTax, totalAmount,
            mapping.HSNCode?.Code, mapping.TaxGroup?.Name);
    }

    /// <summary>Finds the most recently effective active slab matching price and date.</summary>
    private static async Task<TaxSlab?> FindEffectiveSlabAsync(
        AppDbContext context, int taxGroupId, decimal unitPrice, DateTime date, CancellationToken ct)
    {
        return await context.TaxSlabs
            .AsNoTracking()
            .Where(s => s.TaxGroupId == taxGroupId
                     && s.IsActive
                     && s.PriceFrom <= unitPrice
                     && s.PriceTo >= unitPrice
                     && s.EffectiveFrom <= date
                     && (s.EffectiveTo == null || s.EffectiveTo >= date))
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    // ── Validation ───────────────────────────────────────────────────

    private static void ValidateSlabDto(TaxSlabDto dto)
    {
        if (dto.GSTPercent < 0 || dto.GSTPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(dto.GSTPercent), "GST percent must be 0–100.");
        if (dto.PriceFrom < 0)
            throw new ArgumentOutOfRangeException(nameof(dto.PriceFrom), "PriceFrom cannot be negative.");
        if (dto.PriceTo < dto.PriceFrom)
            throw new ArgumentException("PriceTo must be ≥ PriceFrom.");
        if (dto.EffectiveTo.HasValue && dto.EffectiveTo < dto.EffectiveFrom)
            throw new ArgumentException("EffectiveTo must be ≥ EffectiveFrom.");
    }

    private static void ValidateHSNDto(HSNCodeDto dto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Code, nameof(dto.Code));
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Description, nameof(dto.Description));

        var code = dto.Code.Trim();
        if (code.Length < 4 || code.Length > 8)
            throw new ArgumentException("HSN code must be 4–8 characters.", nameof(dto.Code));
    }

    private TaxSlab BuildSlab(TaxSlabDto dto) => new()
    {
        TaxGroupId = dto.TaxGroupId,
        GSTPercent = dto.GSTPercent,
        CGSTPercent = dto.GSTPercent / 2m,
        SGSTPercent = dto.GSTPercent / 2m,
        IGSTPercent = dto.GSTPercent,
        PriceFrom = dto.PriceFrom,
        PriceTo = dto.PriceTo,
        EffectiveFrom = dto.EffectiveFrom,
        EffectiveTo = dto.EffectiveTo,
        IsActive = true,
        CreatedDate = regional.Now
    };
}
