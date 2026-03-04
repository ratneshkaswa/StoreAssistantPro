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
            .OrderBy(t => t.TaxName)
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
            .OrderBy(t => t.TaxName)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<TaxMaster?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.TaxMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(TaxMasterDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto);

        using var _ = perf.BeginScope("TaxService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (await context.TaxMasters.AnyAsync(t => t.TaxName == dto.TaxName, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Tax slab '{dto.TaxName}' already exists.");

        var entity = new TaxMaster
        {
            TaxName = dto.TaxName.Trim(),
            TaxRate = dto.TaxRate,
            HSNCode = string.IsNullOrWhiteSpace(dto.HSNCode) ? null : dto.HSNCode.Trim(),
            ApplicableCategory = dto.ApplicableCategory,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        context.TaxMasters.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, TaxMasterDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto);

        using var _ = perf.BeginScope("TaxService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaxMasters.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tax slab with Id {id} not found.");

        if (await context.TaxMasters.AnyAsync(t => t.TaxName == dto.TaxName && t.Id != id, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Tax slab '{dto.TaxName}' already exists.");

        entity.TaxName = dto.TaxName.Trim();
        entity.TaxRate = dto.TaxRate;
        entity.HSNCode = string.IsNullOrWhiteSpace(dto.HSNCode) ? null : dto.HSNCode.Trim();
        entity.ApplicableCategory = dto.ApplicableCategory;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaxService.ToggleActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaxMasters.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tax slab with Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static void ValidateDto(TaxMasterDto dto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.TaxName, nameof(dto.TaxName));

        if (dto.TaxRate < 0 || dto.TaxRate > 100)
            throw new ArgumentOutOfRangeException(nameof(dto.TaxRate), "GST percent must be between 0 and 100.");

        if (!string.IsNullOrWhiteSpace(dto.HSNCode) && (dto.HSNCode.Trim().Length < 4 || dto.HSNCode.Trim().Length > 8))
            throw new ArgumentException("HSN code must be 4–8 digits.", nameof(dto.HSNCode));
    }
}
