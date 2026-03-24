using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Paging;
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

    public async Task<PagedResult<PurchaseOrder>> GetPagedAsync(
        PagedQuery query, string? search = null, PurchaseOrderStatus? status = null,
        DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PurchaseOrderService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(po => po.OrderNumber.Contains(term) ||
                         (po.Supplier != null && po.Supplier.Name.Contains(term)));
        }
        if (status.HasValue) q = q.Where(po => po.Status == status.Value);
        if (from.HasValue) q = q.Where(po => po.OrderDate >= from.Value);
        if (to.HasValue) q = q.Where(po => po.OrderDate <= to.Value.Date.AddDays(1));

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(po => po.OrderDate)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<PurchaseOrder>(items, totalCount, query.Page, query.PageSize);
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

        if (dto.SupplierId <= 0)
            throw new InvalidOperationException("Supplier is required.");

        var supplierExists = await context.Suppliers
            .AsNoTracking()
            .AnyAsync(s => s.Id == dto.SupplierId, ct)
            .ConfigureAwait(false);

        if (!supplierExists)
            throw new InvalidOperationException("Selected supplier no longer exists.");

        foreach (var item in dto.Items)
        {
            if (item.ProductId <= 0)
                throw new InvalidOperationException("Each PO line must include a product.");

            if (item.Quantity <= 0)
                throw new InvalidOperationException("Each PO line must have a quantity greater than zero.");

            if (item.UnitCost <= 0)
                throw new InvalidOperationException("Each PO line must have a unit cost greater than zero.");
        }

        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var existingProductIds = await context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var missingProductId = productIds.Except(existingProductIds).FirstOrDefault();
        if (missingProductId != 0)
            throw new InvalidOperationException($"Product Id {missingProductId} no longer exists.");

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
                    .ConfigureAwait(false)
                    ?? throw new InvalidOperationException(
                        $"Product Id {item.ProductId} no longer exists. Cannot update stock from PO receipt.");

                product.Quantity += line.QuantityReceived;
                product.CostPrice = item.UnitCost; // Update cost price from PO
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

    public async Task<PurchaseOrder> DuplicateAsync(int purchaseOrderId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PurchaseOrderService.DuplicateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var source = await context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Items)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"PO Id {purchaseOrderId} not found.");

        var orderNumber = await GenerateOrderNumberAsync(context, ct);

        var clone = new PurchaseOrder
        {
            OrderNumber = orderNumber,
            SupplierId = source.SupplierId,
            OrderDate = regional.Now,
            ExpectedDate = null,
            Status = PurchaseOrderStatus.Draft,
            Notes = $"Duplicated from {source.OrderNumber}",
            Items = source.Items.Select(i => new PurchaseOrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                QuantityReceived = 0
            }).ToList()
        };

        context.PurchaseOrders.Add(clone);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return clone;
    }

    public async Task<IReadOnlyList<LowStockPOSuggestion>> GetLowStockSuggestionsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PurchaseOrderService.GetLowStockSuggestionsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var products = await context.Products
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Where(p => p.IsActive && p.MinStockLevel > 0 && p.Quantity <= p.MinStockLevel && p.VendorId != null)
            .OrderBy(p => p.Vendor!.Name)
            .ThenBy(p => p.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return products.Select(p => new LowStockPOSuggestion(
            p.VendorId!.Value,
            p.Vendor!.Name,
            p.Id,
            p.Name,
            p.Quantity,
            p.MinStockLevel,
            Math.Max(p.MinStockLevel * 2 - p.Quantity, 1),
            p.CostPrice)).ToList();
    }

    private async Task<string> GenerateOrderNumberAsync(AppDbContext context, CancellationToken ct)
    {
        var today = regional.Now.Date;
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

    // ── PO import from CSV (#223) ────────────────────────────────────

    public async Task<PurchaseOrder> ImportFromCsvAsync(int supplierId, IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0) throw new InvalidOperationException("CSV contains no rows.");

        using var _ = perf.BeginScope("PurchaseOrderService.ImportFromCsvAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var products = await context.Products
            .ToDictionaryAsync(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase, ct)
            .ConfigureAwait(false);

        var items = new List<PurchaseOrderItem>();
        foreach (var row in rows)
        {
            var name = (row.GetValueOrDefault("ProductName") ?? row.GetValueOrDefault("Product") ?? "").Trim();
            if (!products.TryGetValue(name, out var product)) continue;

            int.TryParse(row.GetValueOrDefault("Quantity") ?? row.GetValueOrDefault("Qty") ?? "1", out var qty);
            decimal.TryParse(row.GetValueOrDefault("UnitCost") ?? row.GetValueOrDefault("Cost") ?? "0", out var cost);
            if (qty <= 0) qty = 1;
            if (cost <= 0) cost = product.CostPrice;

            items.Add(new PurchaseOrderItem
            {
                ProductId = product.Id,
                Quantity = qty,
                UnitCost = cost,
                QuantityReceived = 0
            });
        }

        if (items.Count == 0) throw new InvalidOperationException("No valid product rows found in CSV.");

        var orderNumber = await GenerateOrderNumberAsync(context, ct);
        var po = new PurchaseOrder
        {
            OrderNumber = orderNumber,
            SupplierId = supplierId,
            OrderDate = regional.Now,
            Status = PurchaseOrderStatus.Draft,
            Notes = "Imported from CSV",
            Items = items
        };

        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return po;
    }

    // ── PO export to CSV (#224) ──────────────────────────────────────

    public async Task<IReadOnlyList<string>> ExportToCsvLinesAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PurchaseOrderService.ExportToCsvLinesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (from.HasValue) q = q.Where(po => po.OrderDate >= from.Value);
        if (to.HasValue) q = q.Where(po => po.OrderDate <= to.Value.Date.AddDays(1));

        var orders = await q.OrderByDescending(po => po.OrderDate)
            .ToListAsync(ct).ConfigureAwait(false);

        var lines = new List<string>
        {
            "OrderNumber,OrderDate,SupplierName,Status,ProductName,Quantity,UnitCost,LineTotal"
        };

        foreach (var po in orders)
        {
            foreach (var item in po.Items)
            {
                var lineTotal = item.Quantity * item.UnitCost;
                lines.Add($"\"{po.OrderNumber}\",\"{po.OrderDate:yyyy-MM-dd}\",\"{po.Supplier?.Name}\",\"{po.Status}\",\"{item.Product?.Name}\",{item.Quantity},{item.UnitCost},{lineTotal}");
            }
        }

        return lines;
    }

    // ── PO print data (#219/#451) ────────────────────────────────────

    public async Task<PurchaseOrderPrintData?> GetPrintDataAsync(int purchaseOrderId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PurchaseOrderService.GetPrintDataAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var po = await context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct)
            .ConfigureAwait(false);

        if (po is null) return null;

        var printLines = po.Items.Select(i => new PurchaseOrderPrintLine(
            i.Product?.Name ?? "Unknown",
            i.Product?.HSNCode,
            i.Quantity,
            i.UnitCost,
            i.Quantity * i.UnitCost)).ToList();

        return new PurchaseOrderPrintData(
            po.OrderNumber,
            po.OrderDate,
            po.ExpectedDate,
            po.Supplier?.Name ?? "Unknown",
            po.Supplier?.Phone,
            po.Supplier?.GSTIN,
            po.Supplier?.Address,
            po.Notes,
            po.Status.ToString(),
            printLines,
            printLines.Sum(l => l.LineTotal));
    }
}
