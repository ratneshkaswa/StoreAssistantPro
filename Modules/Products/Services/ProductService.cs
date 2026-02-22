using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Data;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public class ProductService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : IProductService
{
    public async Task<PagedResult<Product>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        IQueryable<Product> q = context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            q = q.Where(p => p.Name.Contains(query.SearchTerm));

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var totalPages = query.PageSize > 0
            ? (int)Math.Ceiling((double)totalCount / query.PageSize)
            : 0;

        var pageIndex = Math.Clamp(query.PageIndex, 0, Math.Max(0, totalPages - 1));

        var items = await q
            .OrderBy(p => p.Name)
            .Skip(pageIndex * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<Product>(items, totalCount, pageIndex, query.PageSize);
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.Products.Add(product);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        try
        {
            context.Products.Update(product);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This product was modified by another user. Please reload and try again.");
        }
    }

    public async Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var product = new Product { Id = id, RowVersion = rowVersion };
        context.Products.Attach(product);

        try
        {
            context.Products.Remove(product);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This product was modified by another user. Please reload and try again.");
        }
    }
}
