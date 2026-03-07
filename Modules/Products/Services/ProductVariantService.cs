using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public class ProductVariantService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : IProductVariantService
{
    public async Task<IReadOnlyList<ProductVariant>> GetByProductAsync(int productId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductVariantService.GetByProductAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Size)
            .Include(v => v.Colour)
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Size!.SortOrder)
            .ThenBy(v => v.Colour!.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<ProductVariant?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);

        using var _ = perf.BeginScope("ProductVariantService.GetByBarcodeAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Include(v => v.Size)
            .Include(v => v.Colour)
            .FirstOrDefaultAsync(v => v.Barcode == barcode.Trim() && v.IsActive, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(ProductVariantDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("ProductVariantService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        await ValidateDuplicateAsync(context, dto.ProductId, dto.SizeId, dto.ColourId, null, ct);
        await ValidateBarcodeAsync(context, dto.Barcode, null, ct);

        context.ProductVariants.Add(new ProductVariant
        {
            ProductId = dto.ProductId,
            SizeId = dto.SizeId,
            ColourId = dto.ColourId,
            Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim(),
            Quantity = dto.Quantity,
            AdditionalPrice = dto.AdditionalPrice,
            IsActive = true,
            CreatedDate = regional.Now
        });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, ProductVariantDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("ProductVariantService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Variant Id {id} not found.");

        await ValidateDuplicateAsync(context, dto.ProductId, dto.SizeId, dto.ColourId, id, ct);
        await ValidateBarcodeAsync(context, dto.Barcode, id, ct);

        entity.SizeId = dto.SizeId;
        entity.ColourId = dto.ColourId;
        entity.Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
        entity.Quantity = dto.Quantity;
        entity.AdditionalPrice = dto.AdditionalPrice;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductVariantService.ToggleActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Variant Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductVariantService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Variant Id {id} not found.");

        context.ProductVariants.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task BulkCreateAsync(int productId, IReadOnlyList<int> sizeIds, IReadOnlyList<int> colourIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sizeIds);
        ArgumentNullException.ThrowIfNull(colourIds);

        if (sizeIds.Count == 0 || colourIds.Count == 0)
            throw new InvalidOperationException("Select at least one size and one colour.");

        using var _ = perf.BeginScope("ProductVariantService.BulkCreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var existing = await context.ProductVariants
            .Where(v => v.ProductId == productId)
            .Select(v => new { v.SizeId, v.ColourId })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = existing.ToHashSet();
        var created = 0;

        foreach (var sizeId in sizeIds)
        {
            foreach (var colourId in colourIds)
            {
                if (existingSet.Contains(new { SizeId = sizeId, ColourId = colourId }))
                    continue;

                context.ProductVariants.Add(new ProductVariant
                {
                    ProductId = productId,
                    SizeId = sizeId,
                    ColourId = colourId,
                    Quantity = 0,
                    AdditionalPrice = 0,
                    IsActive = true,
                    CreatedDate = regional.Now
                });
                created++;
            }
        }

        if (created > 0)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static async Task ValidateDuplicateAsync(
        AppDbContext context, int productId, int sizeId, int colourId, int? excludeId, CancellationToken ct)
    {
        var query = context.ProductVariants
            .Where(v => v.ProductId == productId && v.SizeId == sizeId && v.ColourId == colourId);

        if (excludeId.HasValue)
            query = query.Where(v => v.Id != excludeId.Value);

        if (await query.AnyAsync(ct).ConfigureAwait(false))
            throw new InvalidOperationException("A variant with this size + colour combination already exists.");
    }

    private static async Task ValidateBarcodeAsync(
        AppDbContext context, string? barcode, int? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return;

        var trimmed = barcode.Trim();
        var query = context.ProductVariants.Where(v => v.Barcode == trimmed);

        if (excludeId.HasValue)
            query = query.Where(v => v.Id != excludeId.Value);

        if (await query.AnyAsync(ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Barcode '{trimmed}' is already assigned to another variant.");

        // Also check product-level barcodes
        if (await context.Products.AnyAsync(p => p.Barcode == trimmed, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Barcode '{trimmed}' is already assigned to a product.");
    }
}
