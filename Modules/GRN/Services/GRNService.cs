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
}
