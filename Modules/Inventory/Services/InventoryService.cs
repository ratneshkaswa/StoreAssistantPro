using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;

namespace StoreAssistantPro.Modules.Inventory.Services;

public class InventoryService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf,
    IEventBus eventBus) : IInventoryService
{
    // ── Stock adjustment ─────────────────────────────────────────────

    public async Task AdjustStockAsync(StockAdjustmentDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("InventoryService.AdjustStockAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Stock freeze enforcement (#75)
        var config = await context.AppConfigs.AsNoTracking().SingleOrDefaultAsync(ct).ConfigureAwait(false);
        if (config?.IsStockFrozen == true)
            throw new InvalidOperationException("Stock is frozen. Adjustments are not allowed during the audit period.");

        // Stock adjustment approval threshold (#77)
        if (config?.StockAdjustmentApprovalThreshold > 0)
        {
            var diff = Math.Abs(dto.NewQuantity - await GetCurrentQtyAsync(context, dto, ct).ConfigureAwait(false));
            if (diff >= config.StockAdjustmentApprovalThreshold && !dto.IsApproved)
                throw new InvalidOperationException($"Adjustments of {diff}+ units require manager approval.");
        }

        int oldQty;

        if (dto.ProductVariantId.HasValue)
        {
            var variant = await context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == dto.ProductVariantId.Value, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Variant Id {dto.ProductVariantId} not found.");

            oldQty = variant.Quantity;
            variant.Quantity = dto.NewQuantity;
        }
        else
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Product Id {dto.ProductId} not found.");

            oldQty = product.Quantity;
            product.Quantity = dto.NewQuantity;
        }

        context.StockAdjustments.Add(new StockAdjustment
        {
            ProductId = dto.ProductId,
            ProductVariantId = dto.ProductVariantId,
            OldQuantity = oldQty,
            NewQuantity = dto.NewQuantity,
            Reason = dto.Reason,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            UserId = dto.UserId,
            Timestamp = regional.Now
        });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await PublishInventoryDataChangedAsync("InventoryStockAdjusted").ConfigureAwait(false);
    }

    // ── Batch stock adjustment ─────────────────────────────────────────

    public async Task<int> BatchAdjustStockAsync(IReadOnlyList<StockAdjustmentDto> dtos, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dtos);
        if (dtos.Count == 0) return 0;

        using var _ = perf.BeginScope("InventoryService.BatchAdjustStockAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Stock freeze enforcement (#75)
        var config = await context.AppConfigs.AsNoTracking().SingleOrDefaultAsync(ct).ConfigureAwait(false);
        if (config?.IsStockFrozen == true)
            throw new InvalidOperationException("Stock is frozen. Adjustments are not allowed during the audit period.");

        var productIds = dtos.Select(d => d.ProductId).Distinct().ToList();
        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct)
            .ConfigureAwait(false);

        var now = regional.Now;
        var count = 0;

        foreach (var dto in dtos)
        {
            if (!products.TryGetValue(dto.ProductId, out var product))
                continue;

            var oldQty = product.Quantity;
            if (oldQty == dto.NewQuantity)
                continue;

            product.Quantity = dto.NewQuantity;

            context.StockAdjustments.Add(new StockAdjustment
            {
                ProductId = dto.ProductId,
                ProductVariantId = dto.ProductVariantId,
                OldQuantity = oldQty,
                NewQuantity = dto.NewQuantity,
                Reason = dto.Reason,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                UserId = dto.UserId,
                Timestamp = now
            });
            count++;
        }

        if (count > 0)
        {
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishInventoryDataChangedAsync("InventoryBatchAdjusted").ConfigureAwait(false);
        }

        return count;
    }

    // ── Adjustment log ───────────────────────────────────────────────

    public async Task<IReadOnlyList<StockAdjustment>> GetAdjustmentLogAsync(int productId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InventoryService.GetAdjustmentLogAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.StockAdjustments
            .AsNoTracking()
            .Include(a => a.Product)
            .Include(a => a.ProductVariant)
            .Where(a => a.ProductId == productId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StockAdjustment>> GetRecentAdjustmentsAsync(int count = 50, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InventoryService.GetRecentAdjustmentsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.StockAdjustments
            .AsNoTracking()
            .Include(a => a.Product)
            .Include(a => a.ProductVariant)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    // ── Low stock / Out of stock ─────────────────────────────────────

    public async Task<IReadOnlyList<Product>> GetLowStockProductsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InventoryService.GetLowStockProductsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && p.Quantity > 0 && p.Quantity <= p.MinStockLevel)
            .OrderBy(p => p.Quantity)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Product>> GetOutOfStockProductsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InventoryService.GetOutOfStockProductsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && p.Quantity == 0)
            .OrderBy(p => p.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ProductVariant>> GetLowStockVariantsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InventoryService.GetLowStockVariantsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Include(v => v.Size)
            .Include(v => v.Colour)
            .Where(v => v.IsActive && v.Product!.IsActive
                     && v.Product.MinStockLevel > 0
                     && v.Quantity <= v.Product.MinStockLevel)
            .OrderBy(v => v.Quantity)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    // ── Stock value ──────────────────────────────────────────────────

    public async Task<decimal> GetTotalStockValueAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InventoryService.GetTotalStockValueAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .Where(p => p.IsActive)
            .SumAsync(p => p.Quantity * p.CostPrice, ct)
            .ConfigureAwait(false);
    }

    // ── Stock movement history ───────────────────────────────────────

    public async Task<IReadOnlyList<StockMovementEntry>> GetStockMovementHistoryAsync(int productId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InventoryService.GetStockMovementHistoryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var adjustmentData = await context.StockAdjustments
            .AsNoTracking()
            .Where(a => a.ProductId == productId)
            .Select(a => new { a.Timestamp, a.Reason, a.Notes, a.OldQuantity, a.NewQuantity })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var adjustments = adjustmentData.Select(a => new StockMovementEntry(
            a.Timestamp,
            "Adjustment",
            a.Reason.ToString() + (a.Notes != null ? $" — {a.Notes}" : ""),
            a.NewQuantity - a.OldQuantity,
            null));

        var saleData = await context.SaleItems
            .AsNoTracking()
            .Where(si => si.ProductId == productId)
            .Select(si => new { si.Sale!.SaleDate, si.Quantity, si.Sale.InvoiceNumber })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sales = saleData.Select(si => new StockMovementEntry(
            si.SaleDate,
            "Sale",
            $"Invoice {si.InvoiceNumber}",
            -si.Quantity,
            si.InvoiceNumber));

        var purchaseData = await context.PurchaseOrderItems
            .AsNoTracking()
            .Where(poi => poi.ProductId == productId && poi.QuantityReceived > 0)
            .Select(poi => new { poi.PurchaseOrder!.OrderDate, poi.QuantityReceived, poi.PurchaseOrder.OrderNumber })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var purchases = purchaseData.Select(poi => new StockMovementEntry(
            poi.OrderDate,
            "Purchase",
            $"PO {poi.OrderNumber}",
            poi.QuantityReceived,
            poi.OrderNumber));

        return [.. adjustments.Concat(sales).Concat(purchases).OrderByDescending(e => e.Date)];
    }

    // ── Dead stock ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<Product>> GetDeadStockAsync(int days = 90, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InventoryService.GetDeadStockAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = regional.Now.AddDays(-days);

        var recentlySoldProductIds = await context.SaleItems
            .AsNoTracking()
            .Where(si => si.Sale!.SaleDate >= cutoff)
            .Select(si => si.ProductId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && p.Quantity > 0 && !recentlySoldProductIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    // ── Bulk import ──────────────────────────────────────────────────

    public async Task<int> ImportStockAsync(IReadOnlyList<Dictionary<string, string>> rows, int userId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InventoryService.ImportStockAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var products = await context.Products
            .ToDictionaryAsync(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase, ct)
            .ConfigureAwait(false);

        var now = regional.Now;
        var count = 0;

        foreach (var row in rows)
        {
            var name = (row.GetValueOrDefault("Name") ?? row.GetValueOrDefault("Product") ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || !products.TryGetValue(name, out var product))
                continue;

            var qtyStr = row.GetValueOrDefault("Quantity") ?? row.GetValueOrDefault("Qty") ?? "";
            if (!int.TryParse(qtyStr.Trim(), out var newQty) || newQty < 0)
                continue;

            var oldQty = product.Quantity;
            if (oldQty == newQty)
                continue;

            product.Quantity = newQty;

            context.StockAdjustments.Add(new StockAdjustment
            {
                ProductId = product.Id,
                OldQuantity = oldQty,
                NewQuantity = newQty,
                Reason = AdjustmentReason.Correction,
                Notes = "CSV import",
                UserId = userId,
                Timestamp = now
            });
            count++;
        }

        if (count > 0)
        {
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishInventoryDataChangedAsync("InventoryCsvImported").ConfigureAwait(false);
        }

        return count;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static async Task<int> GetCurrentQtyAsync(AppDbContext context, StockAdjustmentDto dto, CancellationToken ct)
    {
        if (dto.ProductVariantId.HasValue)
        {
            var variant = await context.ProductVariants
                .AsNoTracking()
                .Where(v => v.Id == dto.ProductVariantId.Value)
                .Select(v => v.Quantity)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            return variant;
        }

        return await context.Products
            .AsNoTracking()
            .Where(p => p.Id == dto.ProductId)
            .Select(p => p.Quantity)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    private Task PublishInventoryDataChangedAsync(string reason, CancellationToken ct = default)
        => eventBus.PublishAsync(new SalesDataChangedEvent(reason, DateTime.UtcNow));
}
