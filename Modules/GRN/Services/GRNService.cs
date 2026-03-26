using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.GRN.Services;

public class GRNService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf,
    IRegionalSettingsService regional) : IGRNService
{
    public async Task<PagedResult<GoodsReceivedNote>> GetPagedAsync(
        PagedQuery query, string? search = null, GRNStatus? status = null,
        DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("GRNService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.GoodsReceivedNotes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Supplier)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(g => g.GRNNumber.Contains(term) ||
                         (g.Supplier != null && g.Supplier.Name.Contains(term)) ||
                         (g.PurchaseOrder != null && g.PurchaseOrder.OrderNumber.Contains(term)));
        }
        if (status.HasValue) q = q.Where(g => g.Status == status.Value);
        if (from.HasValue) q = q.Where(g => g.ReceivedDate >= from.Value);
        if (to.HasValue) q = q.Where(g => g.ReceivedDate <= to.Value.Date.AddDays(1));

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(g => g.ReceivedDate)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<GoodsReceivedNote>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<GoodsReceivedNote?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("GRNService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.GoodsReceivedNotes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Supplier)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(g => g.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<GoodsReceivedNote> CreateFromPOAsync(int purchaseOrderId, string? notes, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("GRNService.CreateFromPOAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var po = await context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Purchase Order Id {purchaseOrderId} not found.");

        var grnNumber = await GenerateGRNNumberAsync(context, ct);

        var grn = new GoodsReceivedNote
        {
            GRNNumber = grnNumber,
            ReceivedDate = regional.Now,
            PurchaseOrderId = purchaseOrderId,
            SupplierId = po.SupplierId,
            Status = GRNStatus.Draft,
            Notes = notes?.Trim(),
            Items = po.Items.Select(i => new GRNItem
            {
                ProductId = i.ProductId,
                QtyExpected = i.Quantity - i.QuantityReceived,
                QtyReceived = 0,
                QtyRejected = 0,
                UnitCost = i.UnitCost
            }).Where(i => i.QtyExpected > 0).ToList()
        };

        context.GoodsReceivedNotes.Add(grn);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return grn;
    }

    public async Task<GoodsReceivedNote> CreateDirectAsync(CreateGRNDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Items.Count == 0) throw new InvalidOperationException("GRN must have at least one item.");

        using var _ = perf.BeginScope("GRNService.CreateDirectAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var grnNumber = await GenerateGRNNumberAsync(context, ct);

        var grn = new GoodsReceivedNote
        {
            GRNNumber = grnNumber,
            ReceivedDate = regional.Now,
            SupplierId = dto.SupplierId,
            Status = GRNStatus.Draft,
            Notes = dto.Notes?.Trim(),
            Items = dto.Items.Select(i => new GRNItem
            {
                ProductId = i.ProductId,
                QtyExpected = i.QtyExpected,
                QtyReceived = 0,
                QtyRejected = 0,
                UnitCost = i.UnitCost
            }).ToList()
        };

        context.GoodsReceivedNotes.Add(grn);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return grn;
    }

    public async Task ConfirmAsync(int grnId, IReadOnlyList<GRNReceiveLine> lines, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("GRNService.ConfirmAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            var grn = await context.GoodsReceivedNotes
                .Include(g => g.Items)
                .FirstOrDefaultAsync(g => g.Id == grnId, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException($"GRN Id {grnId} not found.");

            if (grn.Status != GRNStatus.Draft)
                throw new InvalidOperationException("Only draft GRNs can be confirmed.");

            foreach (var line in lines)
            {
                var item = grn.Items.FirstOrDefault(i => i.Id == line.GRNItemId)
                    ?? throw new InvalidOperationException($"GRN Item {line.GRNItemId} not found.");

                item.QtyReceived = line.QtyReceived;
                item.QtyRejected = line.QtyRejected;

                // #366 — Update product stock
                var product = await context.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId, ct)
                    .ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"Product Id {item.ProductId} not found.");

                product.Quantity += line.QtyReceived;

                // #367 — Update cost price (latest cost)
                if (item.UnitCost > 0)
                    product.CostPrice = item.UnitCost;
            }

            grn.TotalAmount = grn.Items.Sum(i => i.QtyReceived * i.UnitCost);
            grn.Status = GRNStatus.Confirmed;

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task CancelAsync(int grnId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("GRNService.CancelAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var grn = await context.GoodsReceivedNotes
            .FirstOrDefaultAsync(g => g.Id == grnId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"GRN Id {grnId} not found.");

        if (grn.Status == GRNStatus.Confirmed)
            throw new InvalidOperationException("Cannot cancel a confirmed GRN.");

        grn.Status = GRNStatus.Cancelled;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task<string> GenerateGRNNumberAsync(AppDbContext context, CancellationToken ct)
    {
        var today = regional.Now.Date;
        var prefix = $"GRN-{today:yyyyMMdd}-";
        var last = await context.GoodsReceivedNotes
            .Where(g => g.GRNNumber.StartsWith(prefix))
            .OrderByDescending(g => g.GRNNumber)
            .Select(g => g.GRNNumber)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var lastSeq))
            seq = lastSeq + 1;

        return $"{prefix}{seq:D4}";
    }

    // ── Quality check (#368) ─────────────────────────────────────────

    public async Task QualityCheckAsync(int grnId, IReadOnlyList<GRNQualityLine> lines, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("GRNService.QualityCheckAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var grn = await context.GoodsReceivedNotes
            .Include(g => g.Items)
            .FirstOrDefaultAsync(g => g.Id == grnId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"GRN Id {grnId} not found.");

        foreach (var line in lines)
        {
            var item = grn.Items.FirstOrDefault(i => i.Id == line.GRNItemId)
                ?? throw new InvalidOperationException($"GRN Item {line.GRNItemId} not found.");

            item.QtyReceived = line.QtyAccepted;
            item.QtyRejected = line.QtyRejected;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Purchase return to supplier (#374) ───────────────────────────

    public async Task<PurchaseReturn> CreatePurchaseReturnAsync(CreatePurchaseReturnDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Items.Count == 0) throw new InvalidOperationException("Purchase return must have at least one item.");

        using var _ = perf.BeginScope("GRNService.CreatePurchaseReturnAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            var returnNumber = await GeneratePurchaseReturnNumberAsync(context, ct);
            var debitNoteNumber = await GenerateDebitNoteNumberAsync(context, ct);

            var purchaseReturn = new PurchaseReturn
            {
                ReturnNumber = returnNumber,
                SupplierId = dto.SupplierId,
                ReturnDate = regional.Now,
                DebitNoteNumber = debitNoteNumber,
                Notes = dto.Notes?.Trim(),
                Items = dto.Items.Select(i => new PurchaseReturnItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    Reason = i.Reason
                }).ToList()
            };

            purchaseReturn.TotalAmount = purchaseReturn.Items.Sum(i => i.Quantity * i.UnitCost);

            // Deduct stock for returned items
            foreach (var item in dto.Items)
            {
                var product = await context.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId, ct)
                    .ConfigureAwait(false);
                if (product is not null)
                    product.Quantity = Math.Max(0, product.Quantity - item.Quantity);
            }

            context.PurchaseReturns.Add(purchaseReturn);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);

            return purchaseReturn;
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<IReadOnlyList<PurchaseReturn>> GetPurchaseReturnsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("GRNService.GetPurchaseReturnsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.PurchaseReturns
            .AsNoTracking()
            .Include(pr => pr.Supplier)
            .Include(pr => pr.Items).ThenInclude(i => i.Product)
            .OrderByDescending(pr => pr.ReturnDate)
            .Take(200)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<string> GeneratePurchaseReturnNumberAsync(AppDbContext context, CancellationToken ct)
    {
        var today = regional.Now.Date;
        var prefix = $"PR-{today:yyyyMMdd}-";
        var last = await context.PurchaseReturns
            .Where(pr => pr.ReturnNumber.StartsWith(prefix))
            .OrderByDescending(pr => pr.ReturnNumber)
            .Select(pr => pr.ReturnNumber)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var lastSeq))
            seq = lastSeq + 1;
        return $"{prefix}{seq:D4}";
    }

    private async Task<string> GenerateDebitNoteNumberAsync(AppDbContext context, CancellationToken ct)
    {
        var today = regional.Now.Date;
        var prefix = $"DN-{today:yyyyMMdd}-";
        var last = await context.PurchaseReturns
            .Where(pr => pr.DebitNoteNumber != null && pr.DebitNoteNumber.StartsWith(prefix))
            .OrderByDescending(pr => pr.DebitNoteNumber)
            .Select(pr => pr.DebitNoteNumber)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var lastSeq))
            seq = lastSeq + 1;
        return $"{prefix}{seq:D4}";
    }

    // ── GRN export to CSV (#373) ─────────────────────────────────────

    public async Task<IReadOnlyList<string>> ExportToCsvLinesAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("GRNService.ExportToCsvLinesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.GoodsReceivedNotes
            .AsNoTracking()
            .Include(g => g.Supplier)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (from.HasValue) q = q.Where(g => g.ReceivedDate >= from.Value);
        if (to.HasValue) q = q.Where(g => g.ReceivedDate <= to.Value.Date.AddDays(1));

        var grns = await q.OrderByDescending(g => g.ReceivedDate)
            .ToListAsync(ct).ConfigureAwait(false);

        var lines = new List<string>
        {
            "GRNNumber,ReceivedDate,SupplierName,PONumber,Status,ProductName,QtyExpected,QtyReceived,QtyRejected,UnitCost,LineTotal"
        };

        foreach (var grn in grns)
        {
            foreach (var item in grn.Items)
            {
                var lineTotal = item.QtyReceived * item.UnitCost;
                lines.Add($"\"{grn.GRNNumber}\",\"{grn.ReceivedDate:yyyy-MM-dd}\",\"{grn.Supplier?.Name}\",\"{grn.PurchaseOrder?.OrderNumber}\",\"{grn.Status}\",\"{item.Product?.Name}\",{item.QtyExpected},{item.QtyReceived},{item.QtyRejected},{item.UnitCost},{lineTotal}");
            }
        }

        return lines;
    }

    // ── GRN print data (#371) ────────────────────────────────────────

    public async Task<GRNPrintData?> GetPrintDataAsync(int grnId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("GRNService.GetPrintDataAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var grn = await context.GoodsReceivedNotes
            .AsNoTracking()
            .Include(g => g.Supplier)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(g => g.Id == grnId, ct)
            .ConfigureAwait(false);

        if (grn is null) return null;

        var printLines = grn.Items.Select(i => new GRNPrintLine(
            i.Product?.Name ?? "Unknown",
            i.Product?.HSNCode,
            i.QtyExpected,
            i.QtyReceived,
            i.QtyRejected,
            i.UnitCost,
            i.QtyReceived * i.UnitCost)).ToList();

        return new GRNPrintData(
            grn.GRNNumber,
            grn.ReceivedDate,
            grn.Supplier?.Name ?? "Unknown",
            grn.Supplier?.Phone,
            grn.Supplier?.GSTIN,
            grn.PurchaseOrder?.OrderNumber,
            grn.Notes,
            grn.Status.ToString(),
            printLines,
            printLines.Sum(l => l.LineTotal));
    }
}
