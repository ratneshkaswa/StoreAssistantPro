using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Data;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Sales.Services;

public class SalesService(
    IDbContextFactory<AppDbContext> contextFactory,
    ITransactionHelper transaction,
    IPerformanceMonitor perf) : ISalesService
{
    public async Task<PagedResult<Sale>> GetPagedAsync(
        PagedQuery query, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalesService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        IQueryable<Sale> q = context.Sales
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking();

        if (from is not null)
            q = q.Where(s => s.SaleDate >= from.Value);

        if (to is not null)
            q = q.Where(s => s.SaleDate < to.Value);

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var totalPages = query.PageSize > 0
            ? (int)Math.Ceiling((double)totalCount / query.PageSize)
            : 0;

        var pageIndex = Math.Clamp(query.PageIndex, 0, Math.Max(0, totalPages - 1));

        var items = await q
            .OrderByDescending(s => s.SaleDate)
            .Skip(pageIndex * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<Sale>(items, totalCount, pageIndex, query.PageSize);
    }

    public async Task<IEnumerable<Sale>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Sales
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Sale?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Sales
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<int> CreateSaleAsync(Sale sale, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalesService.CreateSaleAsync");
        return await transaction.ExecuteInTransactionAsync(async context =>
        {
            var saleItems = new List<SaleItem>();
            foreach (var item in sale.Items)
            {
                var product = await context.Products.FindAsync([item.ProductId], ct)
                    ?? throw new InvalidOperationException($"Product {item.ProductId} not found.");

                if (product.Quantity < item.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{product.Name}'. Available: {product.Quantity}.");

                product.Quantity -= item.Quantity;
                saleItems.Add(new SaleItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            var newSale = new Sale
            {
                SaleDate = sale.SaleDate,
                TotalAmount = sale.TotalAmount,
                PaymentMethod = sale.PaymentMethod,
                Items = saleItems
            };
            context.Sales.Add(newSale);

            return newSale.Id;
        }).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Sales
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .Where(s => s.SaleDate >= from && s.SaleDate < to)
            .AsNoTracking()
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
