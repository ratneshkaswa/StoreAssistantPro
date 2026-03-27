using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;

namespace StoreAssistantPro.Modules.Inventory.Services;

public class StockTakeService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IEventBus eventBus) : IStockTakeService
{
    public async Task<StockTake> StartAsync(string? notes, int userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var now = regional.Now;
        var todayCount = await context.StockTakes
            .CountAsync(s => s.StartedAt.Date == now.Date, ct)
            .ConfigureAwait(false);

        var stockTake = new StockTake
        {
            Reference = $"ST-{now:yyyyMMdd}-{todayCount + 1:D3}",
            Status = StockTakeStatus.InProgress,
            StartedAt = now,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            UserId = userId
        };

        var products = await context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Quantity })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var p in products)
        {
            stockTake.Items.Add(new StockTakeItem
            {
                ProductId = p.Id,
                ProductName = p.Name,
                SystemQuantity = p.Quantity,
                IsCounted = false
            });
        }

        stockTake.TotalItems = stockTake.Items.Count;

        context.StockTakes.Add(stockTake);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return stockTake;
    }

    public async Task<StockTake?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.StockTakes
            .AsNoTracking()
            .Include(s => s.Items.OrderBy(i => i.ProductName))
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StockTake>> GetRecentAsync(int count = 20, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.StockTakes
            .AsNoTracking()
            .OrderByDescending(s => s.StartedAt)
            .Take(count)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task UpdateCountAsync(int stockTakeItemId, int countedQty, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var item = await context.Set<StockTakeItem>()
            .FirstOrDefaultAsync(i => i.Id == stockTakeItemId, ct)
            .ConfigureAwait(false);

        if (item is null) return;

        item.CountedQuantity = countedQty;
        item.IsCounted = true;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<StockTakeResult> CompleteAsync(int stockTakeId, int userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var stockTake = await context.StockTakes
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == stockTakeId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Stock take {stockTakeId} not found.");

        if (stockTake.Status != StockTakeStatus.InProgress)
            throw new InvalidOperationException("Only in-progress stock takes can be completed.");

        var now = regional.Now;
        var discrepancies = 0;
        var adjusted = 0;

        var productIds = stockTake.Items
            .Where(i => i.IsCounted)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct)
            .ConfigureAwait(false);

        foreach (var item in stockTake.Items.Where(i => i.IsCounted))
        {
            var variance = (item.CountedQuantity ?? item.SystemQuantity) - item.SystemQuantity;
            if (variance != 0)
            {
                discrepancies++;

                if (products.TryGetValue(item.ProductId, out var product))
                {
                    var oldQty = product.Quantity;
                    product.Quantity = item.CountedQuantity!.Value;

                    context.StockAdjustments.Add(new StockAdjustment
                    {
                        ProductId = item.ProductId,
                        ProductVariantId = item.ProductVariantId,
                        OldQuantity = oldQty,
                        NewQuantity = item.CountedQuantity!.Value,
                        Reason = AdjustmentReason.Correction,
                        Notes = $"Stock take {stockTake.Reference}: physical count adjustment",
                        UserId = userId,
                        Timestamp = now
                    });

                    adjusted++;
                }
            }
        }

        stockTake.Status = StockTakeStatus.Completed;
        stockTake.CompletedAt = now;
        stockTake.DiscrepancyCount = discrepancies;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        if (adjusted > 0)
            await eventBus.PublishAsync(new SalesDataChangedEvent("StockTakeCompleted", DateTime.UtcNow)).ConfigureAwait(false);

        return new StockTakeResult(stockTake.TotalItems, discrepancies, adjusted);
    }

    public async Task CancelAsync(int stockTakeId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var stockTake = await context.StockTakes
            .FirstOrDefaultAsync(s => s.Id == stockTakeId, ct)
            .ConfigureAwait(false);

        if (stockTake is null || stockTake.Status != StockTakeStatus.InProgress) return;

        stockTake.Status = StockTakeStatus.Cancelled;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
