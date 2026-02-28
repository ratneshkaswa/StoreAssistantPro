using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Data;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Commands;

namespace StoreAssistantPro.Modules.Products.Services;

public class ProductService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : IProductService
{
    public async Task<PagedResult<Product>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ProductService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        IQueryable<Product> q = context.Products
            .Include(p => p.Brand)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm;
            q = q.Where(p => p.Name.Contains(term)
                           || (p.Barcode != null && p.Barcode.Contains(term))
                           || (p.HSNCode != null && p.HSNCode.Contains(term)));
        }

        q = query.StockFilter switch
        {
            StockFilter.InStock => q.Where(p => p.Quantity > p.MinStockLevel),
            StockFilter.LowStock => q.Where(p => p.Quantity > 0 && p.Quantity <= p.MinStockLevel),
            StockFilter.OutOfStock => q.Where(p => p.Quantity == 0),
            _ => q
        };

        q = query.ActiveFilter switch
        {
            ActiveFilter.ActiveOnly => q.Where(p => p.IsActive),
            ActiveFilter.InactiveOnly => q.Where(p => !p.IsActive),
            _ => q
        };

        if (query.BrandId.HasValue)
            q = q.Where(p => p.BrandId == query.BrandId.Value);

        if (!string.IsNullOrWhiteSpace(query.ColorFilter))
            q = q.Where(p => p.Color == query.ColorFilter);

        if (query.TaxProfileId.HasValue)
            q = q.Where(p => p.TaxProfileId == query.TaxProfileId.Value);

        if (!string.IsNullOrWhiteSpace(query.UomFilter))
            q = q.Where(p => p.UOM == query.UomFilter);

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var totalPages = query.PageSize > 0
            ? (int)Math.Ceiling((double)totalCount / query.PageSize)
            : 0;

        var pageIndex = Math.Clamp(query.PageIndex, 0, Math.Max(0, totalPages - 1));

        var sorted = ApplySort(q, query.SortColumn, query.SortDescending);

        var items = await sorted
            .Skip(pageIndex * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var brandIds = items
            .Where(p => p.BrandId.HasValue)
            .Select(p => p.BrandId!.Value)
            .Distinct()
            .ToList();

        if (brandIds.Count > 0)
        {
            var brandCounts = await context.Products
                .Where(p => brandIds.Contains(p.BrandId!.Value))
                .GroupBy(p => p.BrandId!.Value)
                .Select(g => new { BrandId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BrandId, x => x.Count, ct)
                .ConfigureAwait(false);

            foreach (var item in items)
            {
                if (item.Brand is not null && brandCounts.TryGetValue(item.Brand.Id, out var count))
                    item.Brand.ProductCount = count;
            }
        }

        return new PagedResult<Product>(items, totalCount, pageIndex, query.PageSize);
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .Include(p => p.Brand)
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

    public async Task<int> AddRangeAsync(IReadOnlyList<Product> products, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.Products.AddRange(products);
        return await context.SaveChangesAsync(ct).ConfigureAwait(false);
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

    public async Task<(int Deleted, IReadOnlyList<string> FailedNames)> DeleteRangeAsync(
        IReadOnlyList<BulkDeleteItem> items, CancellationToken ct = default)
    {
        var deleted = 0;
        var failedNames = new List<string>();

        foreach (var item in items)
        {
            try
            {
                await DeleteAsync(item.Id, item.RowVersion, ct).ConfigureAwait(false);
                deleted++;
            }
            catch (InvalidOperationException)
            {
                failedNames.Add(item.Name);
            }
        }

        return (deleted, failedNames);
    }

    public async Task<int> GetLowStockCountAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .Where(p => p.IsActive && p.Quantity > 0 && p.Quantity <= p.MinStockLevel)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeProductId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return true;
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Products.Where(p => p.Barcode == barcode.Trim());
        if (excludeProductId.HasValue)
            query = query.Where(p => p.Id != excludeProductId.Value);
        return !await query.AnyAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Product>> FindByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return [];
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .Where(p => p.Barcode == barcode.Trim() && p.IsActive)
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Product>> FindByExactTextAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var trimmed = text.Trim();
        return await context.Products
            .Where(p => p.IsActive && p.Name == trimmed)
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<int> BulkUpdatePricesAsync(int? categoryId, decimal percentage, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Products.Where(p => p.IsActive);
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);
        var products = await query.ToListAsync(ct).ConfigureAwait(false);
        if (products.Count == 0) return 0;
        var factor = 1 + percentage / 100m;
        foreach (var p in products)
            p.SalePrice = Math.Round(p.SalePrice * factor, 2);
        return await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static IOrderedQueryable<Product> ApplySort(
        IQueryable<Product> query, string? sortColumn, bool descending)
    {
        return sortColumn?.ToLowerInvariant() switch
        {
            "id"          => descending ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id),
            "name"        => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "brand"       => descending ? query.OrderByDescending(p => p.Brand!.Name) : query.OrderBy(p => p.Brand!.Name),
            "color"       => descending ? query.OrderByDescending(p => p.Color) : query.OrderBy(p => p.Color),
            "hsncode"     => descending ? query.OrderByDescending(p => p.HSNCode) : query.OrderBy(p => p.HSNCode),
            "barcode"     => descending ? query.OrderByDescending(p => p.Barcode) : query.OrderBy(p => p.Barcode),
            "saleprice"   => descending ? query.OrderByDescending(p => p.SalePrice) : query.OrderBy(p => p.SalePrice),
            "costprice"   => descending ? query.OrderByDescending(p => p.CostPrice) : query.OrderBy(p => p.CostPrice),
            "quantity"    => descending ? query.OrderByDescending(p => p.Quantity) : query.OrderBy(p => p.Quantity),
            "uom"         => descending ? query.OrderByDescending(p => p.UOM) : query.OrderBy(p => p.UOM),
            "isactive"    => descending ? query.OrderByDescending(p => p.IsActive) : query.OrderBy(p => p.IsActive),
            _             => query.OrderBy(p => p.Name)
        };
    }
}
