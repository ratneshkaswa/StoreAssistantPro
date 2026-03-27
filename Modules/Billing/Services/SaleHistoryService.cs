using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

public class SaleHistoryService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf,
    IReferenceDataCache referenceDataCache) : ISaleHistoryService
{
    private static readonly TimeSpan HistoryQueryTtl = TimeSpan.FromSeconds(20);
    private int _cacheVersion;

    public void InvalidateCache() => Interlocked.Increment(ref _cacheVersion);

    public async Task<IReadOnlyList<Sale>> GetSalesAsync(
        DateTime? from, DateTime? to, string? invoiceSearch, CancellationToken ct = default)
    {
        return await referenceDataCache.GetOrCreateValueAsync(
            BuildCacheKey("sales", from, to, invoiceSearch, page: null, pageSize: null, saleId: null),
            async innerCt =>
            {
                using var _ = perf.BeginScope("SaleHistoryService.GetSalesAsync");
                await using var context = await contextFactory.CreateDbContextAsync(innerCt).ConfigureAwait(false);

                var query = ApplyFilters(context.Sales.AsNoTracking().Include(s => s.Items), from, to, invoiceSearch);
                return (IReadOnlyList<Sale>)await query
                    .OrderByDescending(s => s.SaleDate)
                    .Take(500)
                    .ToListAsync(innerCt)
                    .ConfigureAwait(false);
            },
            HistoryQueryTtl,
            ct).ConfigureAwait(false);
    }

    public async Task<PagedResult<Sale>> GetPagedAsync(
        PagedQuery query, DateTime? from = null, DateTime? to = null,
        string? invoiceSearch = null, CancellationToken ct = default)
    {
        return await referenceDataCache.GetOrCreateValueAsync(
            BuildCacheKey("paged", from, to, invoiceSearch, query.Page, query.PageSize, saleId: null),
            async innerCt =>
            {
                using var _ = perf.BeginScope("SaleHistoryService.GetPagedAsync");
                await using var context = await contextFactory.CreateDbContextAsync(innerCt).ConfigureAwait(false);

                var q = ApplyFilters(context.Sales.AsNoTracking().Include(s => s.Items), from, to, invoiceSearch);
                var totalCount = await q.CountAsync(innerCt).ConfigureAwait(false);

                var items = await q
                    .OrderByDescending(s => s.SaleDate)
                    .Skip(query.Skip)
                    .Take(query.PageSize)
                    .ToListAsync(innerCt)
                    .ConfigureAwait(false);

                return new PagedResult<Sale>(items, totalCount, query.Page, query.PageSize);
            },
            HistoryQueryTtl,
            ct).ConfigureAwait(false);
    }

    public async Task<Sale?> GetSaleDetailAsync(int saleId, CancellationToken ct = default)
    {
        return await referenceDataCache.GetOrCreateValueAsync(
            BuildCacheKey("detail", null, null, null, page: null, pageSize: null, saleId),
            async innerCt =>
            {
                using var _ = perf.BeginScope("SaleHistoryService.GetSaleDetailAsync");
                await using var context = await contextFactory.CreateDbContextAsync(innerCt).ConfigureAwait(false);

                return await context.Sales
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(s => s.Items).ThenInclude(i => i.Product)
                    .Include(s => s.Customer)
                    .FirstOrDefaultAsync(s => s.Id == saleId, innerCt)
                    .ConfigureAwait(false);
            },
            HistoryQueryTtl,
            ct).ConfigureAwait(false);
    }

    private static IQueryable<Sale> ApplyFilters(
        IQueryable<Sale> query,
        DateTime? from,
        DateTime? to,
        string? invoiceSearch)
    {
        if (from.HasValue)
            query = query.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.SaleDate <= to.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(invoiceSearch))
            query = query.Where(s => s.InvoiceNumber.Contains(invoiceSearch.Trim()));

        return query;
    }

    private string BuildCacheKey(
        string scope,
        DateTime? from,
        DateTime? to,
        string? invoiceSearch,
        int? page,
        int? pageSize,
        int? saleId) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"sale-history:{Volatile.Read(ref _cacheVersion)}:{scope}:{from:O}:{to:O}:{invoiceSearch?.Trim() ?? string.Empty}:{page}:{pageSize}:{saleId}");
}
