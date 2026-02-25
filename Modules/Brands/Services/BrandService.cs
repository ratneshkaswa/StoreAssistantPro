using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Brands.Services;

public class BrandService(IDbContextFactory<AppDbContext> contextFactory) : IBrandService
{
    public async Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Brand>> GetAllWithProductCountAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Brands
            .Include(b => b.Products)
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Brand brand, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.Brands.Add(brand);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Brand brand, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        try
        {
            context.Brands.Update(brand);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This brand was modified by another user. Please reload and try again.");
        }
    }

    public async Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var brand = new Brand { Id = id, RowVersion = rowVersion };
        context.Brands.Attach(brand);

        try
        {
            context.Brands.Remove(brand);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This brand was modified by another user. Please reload and try again.");
        }
    }

    public async Task<bool> HasProductsAsync(int brandId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .AnyAsync(p => p.BrandId == brandId, ct)
            .ConfigureAwait(false);
    }
}
