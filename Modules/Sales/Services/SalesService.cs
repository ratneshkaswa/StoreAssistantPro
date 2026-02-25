using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Data;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Modules.Sales.Services;

public class SalesService(
    IDbContextFactory<AppDbContext> contextFactory,
    ITransactionSafetyService transactionSafety,
    IBillingSaveLockService saveLock,
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

        var sorted = ApplySort(q, query.SortColumn, query.SortDescending);
        var items = await sorted
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

    public async Task<TransactionResult<int>> CreateSaleAsync(Sale sale, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SalesService.CreateSaleAsync");

        // Serialise billing saves — only one save runs at a time.
        await using var guard = await saveLock.AcquireAsync(ct).ConfigureAwait(false);

        // ── Pre-transaction: build entities (no DB, no locks) ──────
        // Prepare detached SaleItem list and the Sale header before
        // entering the transaction so that object allocation, LINQ
        // projections, and validation that doesn't need a consistent
        // DB snapshot all happen outside the lock window.
        var saleItems = sale.Items.Select(i => new SaleItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();

        var newSale = new Sale
        {
            IdempotencyKey = sale.IdempotencyKey,
            InvoiceNumber = sale.InvoiceNumber,
            SaleDate = sale.SaleDate,
            TotalAmount = sale.TotalAmount,
            PaymentMethod = sale.PaymentMethod,
            DiscountType = sale.DiscountType,
            DiscountValue = sale.DiscountValue,
            DiscountAmount = sale.DiscountAmount,
            DiscountReason = sale.DiscountReason,
            Items = saleItems
        };

        // ── Transaction: only DB reads + writes ────────────────────
        return await transactionSafety.ExecuteSafeAsync(async context =>
        {
            // 0. Idempotency guard — must be inside transaction to
            //    prevent TOCTOU race with the subsequent insert.
            var existing = await context.Sales
                .AsNoTracking()
                .Where(s => s.IdempotencyKey == sale.IdempotencyKey)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (existing is not null)
                return existing.Value;

            // 1. Validate stock & deduct quantities — each FindAsync
            //    takes a row-level lock (or snapshot read) that is
            //    held only until commit.
            foreach (var item in saleItems)
            {
                var product = await context.Products.FindAsync([item.ProductId], ct)
                    .ConfigureAwait(false)
                    ?? throw new InvalidOperationException(
                        $"Product {item.ProductId} not found.");

                if (product.Quantity < item.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{product.Name}'. " +
                        $"Available: {product.Quantity}.");

                product.Quantity -= item.Quantity;
            }

            // 2. Attach pre-built Sale + Items (no allocation here)
            context.Sales.Add(newSale);

            // SaveChanges + Commit handled by TransactionSafetyService
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

    private static IOrderedQueryable<Sale> ApplySort(IQueryable<Sale> query, string? sortColumn, bool descending)
    {
        return sortColumn?.ToLowerInvariant() switch
        {
            "id" => descending ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id),
            "invoicenumber" => descending ? query.OrderByDescending(s => s.InvoiceNumber) : query.OrderBy(s => s.InvoiceNumber),
            "saledate" => descending ? query.OrderByDescending(s => s.SaleDate) : query.OrderBy(s => s.SaleDate),
            "totalamount" => descending ? query.OrderByDescending(s => s.TotalAmount) : query.OrderBy(s => s.TotalAmount),
            "paymentmethod" => descending ? query.OrderByDescending(s => s.PaymentMethod) : query.OrderBy(s => s.PaymentMethod),
            "cashierrole" => descending ? query.OrderByDescending(s => s.CashierRole) : query.OrderBy(s => s.CashierRole),
            "itemcount" => descending ? query.OrderByDescending(s => s.Items.Count) : query.OrderBy(s => s.Items.Count),
            _ => query.OrderByDescending(s => s.SaleDate)
        };
    }
}
