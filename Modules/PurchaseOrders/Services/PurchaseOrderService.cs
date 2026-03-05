using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.PurchaseOrders.Services;

public class PurchaseOrderService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf,
    IRegionalSettingsService regional) : IPurchaseOrderService
{
    public async Task<IReadOnlyList<PurchaseOrder>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PurchaseOrderService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.Product)
            .OrderByDescending(po => po.OrderDate)
            .Take(500)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PurchaseOrder>> SearchAsync(
        string? query, PurchaseOrderStatus? status, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PurchaseOrderService.SearchAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(po => po.OrderNumber.Contains(term) ||
                         (po.Supplier != null && po.Supplier.Name.Contains(term)));
        }
        if (status.HasValue) q = q.Where(po => po.Status == status.Value);
        if (from.HasValue) q = q.Where(po => po.OrderDate >= from.Value);
        if (to.HasValue) q = q.Where(po => po.OrderDate <= to.Value.Date.AddDays(1));

        return await q
            .OrderByDescending(po => po.OrderDate)
            .Take(500)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PurchaseOrderService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(po => po.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<PurchaseOrder> CreateAsync(CreatePurchaseOrderDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Items.Count == 0) throw new InvalidOperationException("PO must have at least one item.");

        using var _ = perf.BeginScope("PurchaseOrderService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var orderNumber = await GenerateOrderNumberAsync(context, ct);

        var po = new PurchaseOrder
        {
            OrderNumber = orderNumber,
            OrderDate = regional.Now,
            ExpectedDate = dto.ExpectedDate,
            SupplierId = dto.SupplierId,
            Status = PurchaseOrderStatus.Draft,
            Notes = dto.Notes?.Trim(),
            Items = dto.Items.Select(i => new PurchaseOrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList()
        };

        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return po;
    }

    public async Task UpdateStatusAsync(int id, PurchaseOrderStatus status, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PurchaseOrderService.UpdateStatusAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var po = await context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"PO Id {id} not found.");

        po.Status = status;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ReceiveItemsAsync(int poId, IReadOnlyList<ReceiveLineDto> lines, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PurchaseOrderService.ReceiveItemsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            var po = await context.PurchaseOrders
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == poId, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException($"PO Id {poId} not found.");

            foreach (var line in lines)
            {
                var item = po.Items.FirstOrDefault(i => i.Id == line.PurchaseOrderItemId)
                    ?? throw new InvalidOperationException($"PO Item {line.PurchaseOrderItemId} not found.");

                var maxReceivable = item.Quantity - item.QuantityReceived;
                if (line.QuantityReceived > maxReceivable)
                    throw new InvalidOperationException(
                        $"Cannot receive {line.QuantityReceived} for item {item.Id}. Max: {maxReceivable}.");

                item.QuantityReceived += line.QuantityReceived;

                // Update stock
                var product = await context.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId, ct)
                    .ConfigureAwait(false);
                if (product is not null)
                {
                    product.Quantity += line.QuantityReceived;
                    product.CostPrice = item.UnitCost; // Update cost price from PO
                }
            }

            // Update PO status
            var allReceived = po.Items.All(i => i.QuantityReceived >= i.Quantity);
            var anyReceived = po.Items.Any(i => i.QuantityReceived > 0);
            po.Status = allReceived ? PurchaseOrderStatus.Received
                       : anyReceived ? PurchaseOrderStatus.PartialReceived
                       : po.Status;

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<IReadOnlyList<Supplier>> GetActiveSuppliersAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PurchaseOrderService.GetActiveSuppliersAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static async Task<string> GenerateOrderNumberAsync(AppDbContext context, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"PO-{today:yyyyMMdd}-";
        var last = await context.PurchaseOrders
            .Where(po => po.OrderNumber.StartsWith(prefix))
            .OrderByDescending(po => po.OrderNumber)
            .Select(po => po.OrderNumber)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var seq))
            next = seq + 1;
        return $"{prefix}{next:D4}";
    }
}
