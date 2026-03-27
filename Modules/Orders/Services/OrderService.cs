using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;

namespace StoreAssistantPro.Modules.Orders.Services;

public class OrderService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf,
    IEventBus eventBus) : IOrderService
{
    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("OrderService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.Date)
            .ThenByDescending(o => o.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("OrderService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Orders
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(OrderDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.CustomerName);

        using var _ = perf.BeginScope("OrderService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new Order
        {
            Date = dto.Date,
            CustomerName = dto.CustomerName.Trim(),
            ItemDescription = dto.ItemDescription?.Trim() ?? string.Empty,
            Quantity = dto.Quantity,
            Rate = dto.Rate,
            Amount = dto.Amount,
            Status = "Pending",
            DeliveryDate = dto.DeliveryDate,
            CreatedAt = regional.Now
        };

        context.Orders.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await PublishBusinessDataChangedAsync("OrderCreated").ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, OrderDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("OrderService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Order with Id {id} not found.");

        entity.Date = dto.Date;
        entity.CustomerName = dto.CustomerName.Trim();
        entity.ItemDescription = dto.ItemDescription?.Trim() ?? string.Empty;
        entity.Quantity = dto.Quantity;
        entity.Rate = dto.Rate;
        entity.Amount = dto.Amount;
        entity.DeliveryDate = dto.DeliveryDate;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await PublishBusinessDataChangedAsync("OrderUpdated").ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("OrderService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Order with Id {id} not found.");

        context.Orders.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await PublishBusinessDataChangedAsync("OrderDeleted").ConfigureAwait(false);
    }

    public async Task SetStatusAsync(int id, string status, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        using var _ = perf.BeginScope("OrderService.SetStatusAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Order with Id {id} not found.");

        entity.Status = status;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await PublishBusinessDataChangedAsync("OrderStatusUpdated").ConfigureAwait(false);
    }

    public async Task<OrderStats> GetStatsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("OrderService.GetStatsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var all = await context.Orders.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        return new OrderStats(
            all.Count(o => o.Status == "Pending"),
            all.Count(o => o.Status != "Delivered" && o.Status != "Cancelled"),
            all.Count(o => o.Status == "Delivered"),
            all.Count);
    }

    private Task PublishBusinessDataChangedAsync(string reason)
        => eventBus.PublishAsync(new SalesDataChangedEvent(reason, DateTime.UtcNow));
}
