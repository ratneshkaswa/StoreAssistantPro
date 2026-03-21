using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Quotations.Services;

public class QuotationService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf,
    IRegionalSettingsService regional) : IQuotationService
{
    public async Task<PagedResult<Quotation>> GetPagedAsync(
        PagedQuery query, string? search = null, QuotationStatus? status = null,
        DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("QuotationService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.Quotations
            .AsNoTracking()
            .Include(qt => qt.Customer)
            .Include(qt => qt.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(qt => qt.QuoteNumber.Contains(term) ||
                         (qt.Customer != null && qt.Customer.Name.Contains(term)));
        }
        if (status.HasValue) q = q.Where(qt => qt.Status == status.Value);
        if (from.HasValue) q = q.Where(qt => qt.QuoteDate >= from.Value);
        if (to.HasValue) q = q.Where(qt => qt.QuoteDate <= to.Value.Date.AddDays(1));

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(qt => qt.QuoteDate)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<Quotation>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<Quotation?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("QuotationService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Quotations
            .AsNoTracking()
            .Include(qt => qt.Customer)
            .Include(qt => qt.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(qt => qt.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<Quotation> CreateAsync(CreateQuotationDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Items.Count == 0) throw new InvalidOperationException("Quotation must have at least one item.");

        using var _ = perf.BeginScope("QuotationService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var quoteNumber = await GenerateQuoteNumberAsync(context, ct);

        var quotation = new Quotation
        {
            QuoteNumber = quoteNumber,
            QuoteDate = regional.Now,
            ValidUntil = dto.ValidUntil,
            CustomerId = dto.CustomerId,
            Status = QuotationStatus.Draft,
            Notes = dto.Notes?.Trim(),
            Items = dto.Items.Select(i =>
            {
                var discountedPrice = i.UnitPrice - (i.UnitPrice * i.DiscountRate / 100m);
                var lineSubtotal = i.Quantity * discountedPrice;
                var taxAmount = lineSubtotal * i.TaxRate / 100m;
                var cessAmount = lineSubtotal * i.CessRate / 100m;

                return new QuotationItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountRate = i.DiscountRate,
                    TaxRate = i.TaxRate,
                    TaxAmount = taxAmount,
                    CessRate = i.CessRate,
                    CessAmount = cessAmount
                };
            }).ToList()
        };

        quotation.TotalAmount = quotation.Items.Sum(i => i.Subtotal + i.TaxAmount + i.CessAmount);

        context.Quotations.Add(quotation);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return quotation;
    }

    public async Task UpdateStatusAsync(int id, QuotationStatus status, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("QuotationService.UpdateStatusAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var quotation = await context.Quotations
            .FirstOrDefaultAsync(q => q.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Quotation Id {id} not found.");

        quotation.Status = status;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ExpireOverdueAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("QuotationService.ExpireOverdueAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var now = regional.Now;
        var overdue = await context.Quotations
            .Where(q => q.ValidUntil < now &&
                        q.Status != QuotationStatus.Expired &&
                        q.Status != QuotationStatus.ConvertedToSale &&
                        q.Status != QuotationStatus.Rejected)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var q in overdue)
            q.Status = QuotationStatus.Expired;

        if (overdue.Count > 0)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<QuotationCartLine>> GetCartLinesAsync(int quotationId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("QuotationService.GetCartLinesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var quotation = await context.Quotations
            .AsNoTracking()
            .Include(q => q.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(q => q.Id == quotationId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Quotation Id {quotationId} not found.");

        return quotation.Items.Select(i => new QuotationCartLine(
            i.ProductId,
            i.Product?.Name ?? $"Product #{i.ProductId}",
            i.Quantity,
            i.UnitPrice,
            i.TaxRate,
            i.CessRate)).ToList();
    }

    public async Task MarkConvertedAsync(int quotationId, int saleId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("QuotationService.MarkConvertedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var quotation = await context.Quotations
            .FirstOrDefaultAsync(q => q.Id == quotationId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Quotation Id {quotationId} not found.");

        quotation.Status = QuotationStatus.ConvertedToSale;
        quotation.ConvertedSaleId = saleId;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task<string> GenerateQuoteNumberAsync(AppDbContext context, CancellationToken ct)
    {
        var today = regional.Now.Date;
        var prefix = $"QT-{today:yyyyMMdd}-";
        var last = await context.Quotations
            .Where(q => q.QuoteNumber.StartsWith(prefix))
            .OrderByDescending(q => q.QuoteNumber)
            .Select(q => q.QuoteNumber)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var lastSeq))
            seq = lastSeq + 1;

        return $"{prefix}{seq:D4}";
    }
}
