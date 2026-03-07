using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inventory.Services;

public class InventoryService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : IInventoryService
{
    // ── Stock adjustment ─────────────────────────────────────────────

    public async Task AdjustStockAsync(StockAdjustmentDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("InventoryService.AdjustStockAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

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
}
