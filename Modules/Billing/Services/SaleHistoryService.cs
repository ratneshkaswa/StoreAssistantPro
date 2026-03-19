using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

public class SaleHistoryService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : ISaleHistoryService
{
    public async Task<IReadOnlyList<Sale>> GetSalesAsync(
        DateTime? from, DateTime? to, string? invoiceSearch, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SaleHistoryService.GetSalesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var query = context.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.SaleDate <= to.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(invoiceSearch))
            query = query.Where(s => s.InvoiceNumber.Contains(invoiceSearch.Trim()));

        return await query
            .OrderByDescending(s => s.SaleDate)
            .Take(500)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<Sale>> GetPagedAsync(
        PagedQuery query, DateTime? from = null, DateTime? to = null,
        string? invoiceSearch = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SaleHistoryService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .AsQueryable();

        if (from.HasValue)
            q = q.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue)
            q = q.Where(s => s.SaleDate <= to.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(invoiceSearch))
            q = q.Where(s => s.InvoiceNumber.Contains(invoiceSearch.Trim()));

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(s => s.SaleDate)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<Sale>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<Sale?> GetSaleDetailAsync(int saleId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SaleHistoryService.GetSaleDetailAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.Sales
            .AsNoTracking()
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == saleId, ct)
            .ConfigureAwait(false);
    }
}
