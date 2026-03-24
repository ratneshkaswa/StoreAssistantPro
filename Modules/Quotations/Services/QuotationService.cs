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

    public async Task<Quotation> DuplicateAsync(int quotationId, DateTime validUntil, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("QuotationService.DuplicateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var source = await context.Quotations
            .AsNoTracking()
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == quotationId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Quotation Id {quotationId} not found.");

        var quoteNumber = await GenerateQuoteNumberAsync(context, ct);

        var clone = new Quotation
        {
            QuoteNumber = quoteNumber,
            QuoteDate = regional.Now,
            ValidUntil = validUntil,
            CustomerId = source.CustomerId,
            Status = QuotationStatus.Draft,
            Notes = source.Notes,
            Items = source.Items.Select(i => new QuotationItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountRate = i.DiscountRate,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                CessRate = i.CessRate,
                CessAmount = i.CessAmount
            }).ToList()
        };

        clone.TotalAmount = clone.Items.Sum(i => i.Subtotal + i.TaxAmount + i.CessAmount);

        context.Quotations.Add(clone);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return clone;
    }

    public async Task<Quotation> CreateRevisionAsync(int quotationId, DateTime validUntil, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("QuotationService.CreateRevisionAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var source = await context.Quotations
            .AsNoTracking()
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == quotationId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Quotation Id {quotationId} not found.");

        // Determine the original root and next revision number
        var originalId = source.OriginalQuotationId ?? source.Id;
        var maxRevision = await context.Quotations
            .Where(q => q.OriginalQuotationId == originalId || q.Id == originalId)
            .MaxAsync(q => q.RevisionNumber, ct)
            .ConfigureAwait(false);

        var quoteNumber = await GenerateQuoteNumberAsync(context, ct);

        var revision = new Quotation
        {
            QuoteNumber = quoteNumber,
            QuoteDate = regional.Now,
            ValidUntil = validUntil,
            CustomerId = source.CustomerId,
            Status = QuotationStatus.Draft,
            Notes = source.Notes,
            RevisionNumber = maxRevision + 1,
            OriginalQuotationId = originalId,
            Items = source.Items.Select(i => new QuotationItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountRate = i.DiscountRate,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                CessRate = i.CessRate,
                CessAmount = i.CessAmount
            }).ToList()
        };

        revision.TotalAmount = revision.Items.Sum(i => i.Subtotal + i.TaxAmount + i.CessAmount);

        // Mark original as superseded
        var original = await context.Quotations
            .FirstOrDefaultAsync(q => q.Id == source.Id, ct)
            .ConfigureAwait(false);
        if (original is not null && original.Status is QuotationStatus.Draft or QuotationStatus.Sent)
            original.Status = QuotationStatus.Expired;

        context.Quotations.Add(revision);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return revision;
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

    // ── Quotation export to CSV (#359) ───────────────────────────────

    public async Task<IReadOnlyList<string>> ExportToCsvLinesAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("QuotationService.ExportToCsvLinesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.Quotations
            .AsNoTracking()
            .Include(qt => qt.Customer)
            .Include(qt => qt.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (from.HasValue) q = q.Where(qt => qt.QuoteDate >= from.Value);
        if (to.HasValue) q = q.Where(qt => qt.QuoteDate <= to.Value.Date.AddDays(1));

        var quotations = await q.OrderByDescending(qt => qt.QuoteDate)
            .ToListAsync(ct).ConfigureAwait(false);

        var lines = new List<string>
        {
            "QuoteNumber,QuoteDate,ValidUntil,CustomerName,Status,ProductName,Quantity,UnitPrice,DiscountRate,TaxRate,LineTotal"
        };

        foreach (var qt in quotations)
        {
            foreach (var item in qt.Items)
            {
                lines.Add($"\"{qt.QuoteNumber}\",\"{qt.QuoteDate:yyyy-MM-dd}\",\"{qt.ValidUntil:yyyy-MM-dd}\",\"{qt.Customer?.Name}\",\"{qt.Status}\",\"{item.Product?.Name}\",{item.Quantity},{item.UnitPrice},{item.DiscountRate},{item.TaxRate},{item.Subtotal + item.TaxAmount + item.CessAmount}");
            }
        }

        return lines;
    }

    // ── Quotation terms and conditions (#361) ────────────────────────

    public async Task<string> GetTermsAndConditionsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        return config?.QuotationTermsAndConditions ?? string.Empty;
    }

    // ── Quotation print data (#452) ──────────────────────────────────

    public async Task<QuotationPrintData?> GetPrintDataAsync(int quotationId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("QuotationService.GetPrintDataAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var qt = await context.Quotations
            .AsNoTracking()
            .Include(q => q.Customer)
            .Include(q => q.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(q => q.Id == quotationId, ct)
            .ConfigureAwait(false);

        if (qt is null) return null;

        var config = await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";
        var firmAddress = config?.Address ?? "";
        var firmPhone = config?.Phone ?? "";
        var firmGSTIN = config?.GSTNumber;
        var termsAndConditions = config?.QuotationTermsAndConditions ?? "";

        var items = qt.Items.Select(i =>
        {
            var lineTotal = i.Subtotal + i.TaxAmount + i.CessAmount;
            return new QuotationPrintLine(
                i.Product?.Name ?? $"Product #{i.ProductId}",
                i.Product?.HSNCode,
                i.Quantity,
                i.UnitPrice,
                i.DiscountRate,
                i.TaxRate,
                i.CessRate,
                i.Subtotal,
                i.TaxAmount,
                i.CessAmount,
                lineTotal);
        }).ToList();

        return new QuotationPrintData(
            qt.QuoteNumber,
            qt.QuoteDate,
            qt.ValidUntil,
            qt.Customer?.Name,
            qt.Customer?.Phone,
            qt.Customer?.Address,
            qt.Customer?.GSTIN,
            firmName,
            firmAddress,
            firmPhone,
            firmGSTIN,
            qt.Notes,
            termsAndConditions,
            qt.RevisionNumber,
            qt.TotalAmount,
            items);
    }

    // ── Quotation email stub (#360) ──────────────────────────────────

    public Task<bool> SendEmailAsync(int quotationId, string recipientEmail, CancellationToken ct = default)
    {
        // Stub — future SMTP integration. Returns false to indicate email not configured.
        return Task.FromResult(false);
    }
}
