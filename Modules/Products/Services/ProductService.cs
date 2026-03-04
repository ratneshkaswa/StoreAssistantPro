using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public class ProductService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : IProductService
{
    // ── Products ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Tax)
            .OrderBy(p => p.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.GetActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Tax)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Tax)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(ProductDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name, nameof(dto.Name));

        using var _ = perf.BeginScope("ProductService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (await context.Products.AnyAsync(p => p.Name == dto.Name.Trim(), ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Product '{dto.Name}' already exists.");

        var entity = new Product
        {
            Name = dto.Name.Trim(),
            ProductType = dto.ProductType,
            Unit = dto.Unit,
            TaxId = dto.TaxId,
            SupportsColour = dto.SupportsColour,
            SupportsPattern = dto.SupportsPattern,
            SupportsSize = dto.SupportsSize,
            SupportsType = dto.SupportsType,
            IsActive = true
        };

        context.Products.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, ProductDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name, nameof(dto.Name));

        using var _ = perf.BeginScope("ProductService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Products.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Product with Id {id} not found.");

        if (await context.Products.AnyAsync(p => p.Name == dto.Name.Trim() && p.Id != id, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Product '{dto.Name}' already exists.");

        entity.Name = dto.Name.Trim();
        entity.ProductType = dto.ProductType;
        entity.Unit = dto.Unit;
        entity.TaxId = dto.TaxId;
        entity.SupportsColour = dto.SupportsColour;
        entity.SupportsPattern = dto.SupportsPattern;
        entity.SupportsSize = dto.SupportsSize;
        entity.SupportsType = dto.SupportsType;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.ToggleActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Products.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Product with Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task AttachTaxAsync(int productId, int? taxId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.AttachTaxAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == productId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Product with Id {productId} not found.");

        if (taxId.HasValue)
        {
            var exists = await context.TaxMasters
                .AnyAsync(t => t.Id == taxId.Value && t.IsActive, ct)
                .ConfigureAwait(false);

            if (!exists)
                throw new InvalidOperationException("Selected tax does not exist or is inactive.");
        }

        product.TaxId = taxId;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Taxes (for dropdowns) ────────────────────────────────────────

    public async Task<IReadOnlyList<TaxMaster>> GetActiveTaxesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.GetActiveTaxesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.TaxMasters
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SlabPercent)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    // ── Colours (predefined — read-only) ─────────────────────────────

    public async Task<IReadOnlyList<Colour>> GetColoursAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.GetColoursAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Colours
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    // ── Patterns (manual entry) ──────────────────────────────────────

    public async Task<IReadOnlyList<ProductPattern>> GetPatternsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.GetPatternsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ProductPatterns
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreatePatternAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var _ = perf.BeginScope("ProductService.CreatePatternAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var trimmed = name.Trim();
        if (await context.ProductPatterns.AnyAsync(p => p.Name == trimmed, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Pattern '{trimmed}' already exists.");

        context.ProductPatterns.Add(new ProductPattern { Name = trimmed });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Sizes (manual entry) ─────────────────────────────────────────

    public async Task<IReadOnlyList<ProductSize>> GetSizesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.GetSizesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ProductSizes
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateSizeAsync(string name, int sortOrder, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var _ = perf.BeginScope("ProductService.CreateSizeAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var trimmed = name.Trim();
        if (await context.ProductSizes.AnyAsync(s => s.Name == trimmed, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Size '{trimmed}' already exists.");

        context.ProductSizes.Add(new ProductSize { Name = trimmed, SortOrder = sortOrder });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Variant Types (manual entry) ─────────────────────────────────

    public async Task<IReadOnlyList<ProductVariantType>> GetVariantTypesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.GetVariantTypesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ProductVariantTypes
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateVariantTypeAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var _ = perf.BeginScope("ProductService.CreateVariantTypeAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var trimmed = name.Trim();
        if (await context.ProductVariantTypes.AnyAsync(t => t.Name == trimmed, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Variant type '{trimmed}' already exists.");

        context.ProductVariantTypes.Add(new ProductVariantType { Name = trimmed });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
