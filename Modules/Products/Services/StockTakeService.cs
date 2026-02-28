using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

/// <inheritdoc />
public class StockTakeService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional) : IStockTakeService
{
    public async Task<IReadOnlyList<StockTakeItem>> LoadStockTakeItemsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new StockTakeItem
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Barcode = p.Barcode,
                SKU = p.SKU,
                SystemQty = p.Quantity
            })
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<int> ApplyStockTakeAsync(IReadOnlyList<StockTakeItem> items, CancellationToken ct = default)
    {
        var discrepancies = items
            .Where(i => i.HasDiscrepancy)
            .ToList();

        if (discrepancies.Count == 0)
            return 0;

        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var now = regional.Now;
        var adjusted = 0;

        foreach (var item in discrepancies)
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId, ct)
                .ConfigureAwait(false);

            if (product is null) continue;

            var diff = item.PhysicalQty!.Value - product.Quantity;
            if (diff == 0) continue;

            context.StockAdjustmentLogs.Add(new StockAdjustmentLog
            {
                ProductId = product.Id,
                ProductName = product.Name,
                QuantityBefore = product.Quantity,
                AdjustmentQty = diff,
                QuantityAfter = item.PhysicalQty.Value,
                Reason = "Stock Take: physical count reconciliation",
                AdjustedAt = now,
                Source = "StockTake"
            });

            product.Quantity = item.PhysicalQty.Value;
            adjusted++;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return adjusted;
    }
}
