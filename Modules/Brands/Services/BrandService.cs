using Microsoft.EntityFrameworkCore;
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
        var brands = await context.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Populate product counts
        foreach (var brand in brands)
        {
            brand.ProductCount = await context.Products
                .CountAsync(p => p.BrandId == brand.Id, ct)
                .ConfigureAwait(false);
        }

        return brands;
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

        foreach (var brand in brands)
        {
            brand.ProductCount = await context.Products
                .CountAsync(p => p.BrandId == brand.Id, ct)
                .ConfigureAwait(false);
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
}
