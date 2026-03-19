using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Brands.Services;

public class BrandService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : IBrandService
{
    public async Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BrandService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var productCounts = await context.Products
            .Where(p => p.BrandId.HasValue)
            .GroupBy(p => p.BrandId!.Value)
            .Select(g => new { BrandId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.BrandId, g => g.Count, ct)
            .ConfigureAwait(false);

        var brands = await context.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var brand in brands)
            brand.ProductCount = productCounts.GetValueOrDefault(brand.Id);

        return brands;
    }

    public async Task<PagedResult<Brand>> GetPagedAsync(PagedQuery query, string? search = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BrandService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.Brands.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(b => b.Name.Contains(search.Trim()));

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var brands = await q
            .OrderBy(b => b.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (brands.Count > 0)
        {
            var brandIds = brands.Select(b => b.Id).ToList();
            var productCounts = await context.Products
                .Where(p => p.BrandId.HasValue && brandIds.Contains(p.BrandId.Value))
                .GroupBy(p => p.BrandId!.Value)
                .Select(g => new { BrandId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.BrandId, g => g.Count, ct)
                .ConfigureAwait(false);

            foreach (var brand in brands)
                brand.ProductCount = productCounts.GetValueOrDefault(brand.Id);
        }

        return new PagedResult<Brand>(brands, totalCount, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<Brand>> GetActiveAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BrandService.GetActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Brands
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Brand>> SearchAsync(string query, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        using var _ = perf.BeginScope("BrandService.SearchAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var term = query.Trim();
        var brands = await context.Brands
            .AsNoTracking()
            .Where(b => b.Name.Contains(term))
            .OrderBy(b => b.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (brands.Count > 0)
        {
            var brandIds = brands.Select(b => b.Id).ToList();
            var productCounts = await context.Products
                .Where(p => p.BrandId.HasValue && brandIds.Contains(p.BrandId.Value))
                .GroupBy(p => p.BrandId!.Value)
                .Select(g => new { BrandId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.BrandId, g => g.Count, ct)
                .ConfigureAwait(false);

            foreach (var brand in brands)
                brand.ProductCount = productCounts.GetValueOrDefault(brand.Id);
        }

        return brands;
    }

    public async Task CreateAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var _ = perf.BeginScope("BrandService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var trimmed = name.Trim();
        if (await context.Brands.AnyAsync(b => b.Name == trimmed, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Brand '{trimmed}' already exists.");

        context.Brands.Add(new Brand { Name = trimmed, IsActive = true });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var _ = perf.BeginScope("BrandService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Brands.FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Brand Id {id} not found.");

        var trimmed = name.Trim();
        if (await context.Brands.AnyAsync(b => b.Name == trimmed && b.Id != id, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Brand '{trimmed}' already exists.");

        entity.Name = trimmed;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BrandService.ToggleActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Brands.FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Brand Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<int> ImportBulkAsync(IReadOnlyList<string> names, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BrandService.ImportBulkAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var existing = await context.Brands
            .Select(b => b.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        int imported = 0;

        foreach (var name in names)
        {
            var trimmed = name.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || !existingSet.Add(trimmed))
                continue;

            context.Brands.Add(new Brand { Name = trimmed, IsActive = true });
            imported++;
        }

        if (imported > 0)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return imported;
    }
}
