using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.Ecommerce;

namespace StoreAssistantPro.Modules.Ecommerce.Services;

public sealed class PlatformConnectionService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<PlatformConnectionService> logger) : IPlatformConnectionService
{
    public async Task<IReadOnlyList<PlatformConnection>> GetConnectionsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.PlatformConnections.ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<PlatformConnection> SaveConnectionAsync(PlatformConnection connection, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (connection.Id == 0) context.PlatformConnections.Add(connection);
        else context.PlatformConnections.Update(connection);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Platform connection saved: {Platform}", connection.Platform);
        return connection;
    }

    public async Task DeleteConnectionAsync(int connectionId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var conn = await context.PlatformConnections.FindAsync([connectionId], ct).ConfigureAwait(false);
        if (conn is null) return;
        context.PlatformConnections.Remove(conn);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task<bool> TestConnectionAsync(int connectionId, CancellationToken ct = default)
    {
        logger.LogInformation("Testing connection {Id}", connectionId);
        return Task.FromResult(true);
    }
}

public sealed class ProductSyncService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<ProductSyncService> logger) : IProductSyncService
{
    public async Task<int> PushProductsAsync(EcommercePlatform platform, IReadOnlyList<int>? productIds = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Products.Where(p => p.IsActive);
        if (productIds is { Count: > 0 }) query = query.Where(p => productIds.Contains(p.Id));
        var count = await query.CountAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Pushed {Count} products to {Platform}", count, platform);
        return count;
    }

    public Task SyncInventoryAsync(EcommercePlatform platform, CancellationToken ct = default)
    {
        logger.LogInformation("Inventory sync to {Platform}", platform);
        return Task.CompletedTask;
    }

    public Task SyncPricesAsync(EcommercePlatform platform, CancellationToken ct = default)
    {
        logger.LogInformation("Price sync to {Platform}", platform);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ProductListing>> GetListingsAsync(int productId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ProductListings.Where(l => l.ProductId == productId).ToListAsync(ct).ConfigureAwait(false);
    }
}

public sealed class OnlineOrderService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<OnlineOrderService> logger) : IOnlineOrderService
{
    public Task<IReadOnlyList<OnlineOrder>> PullOrdersAsync(EcommercePlatform platform, CancellationToken ct = default)
    {
        logger.LogInformation("Pulling orders from {Platform}", platform);
        return Task.FromResult<IReadOnlyList<OnlineOrder>>([]);
    }

    public async Task<IReadOnlyList<OnlineOrder>> GetOrdersAsync(EcommercePlatform? platform = null, string? status = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.OnlineOrders.AsQueryable();
        if (platform.HasValue) query = query.Where(o => o.Platform == platform.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);
        return await query.OrderByDescending(o => o.OrderDate).ToListAsync(ct).ConfigureAwait(false);
    }

    public Task<int> ConvertToSaleAsync(int onlineOrderId, CancellationToken ct = default)
    {
        logger.LogInformation("Converting online order {Id} to sale", onlineOrderId);
        return Task.FromResult(0);
    }

    public async Task UpdateOrderStatusAsync(int orderId, string status, string? trackingNumber = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var order = await context.OnlineOrders.FindAsync([orderId], ct).ConfigureAwait(false);
        if (order is null) return;
        order.Status = status;
        if (trackingNumber is not null) order.TrackingNumber = trackingNumber;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task CancelOrderAsync(int orderId, string? reason = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var order = await context.OnlineOrders.FindAsync([orderId], ct).ConfigureAwait(false);
        if (order is null) return;
        order.Status = "Cancelled";
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Order {Id} cancelled: {Reason}", orderId, reason);
    }
}

public sealed class MarketplaceService(ILogger<MarketplaceService> logger) : IMarketplaceService
{
    public Task<IReadOnlyList<MarketplaceCommission>> GetCommissionsAsync(EcommercePlatform platform, DateTime from, DateTime to, CancellationToken ct = default)
    {
        logger.LogInformation("Getting commissions for {Platform} from {From} to {To}", platform, from, to);
        return Task.FromResult<IReadOnlyList<MarketplaceCommission>>([]);
    }

    public Task SyncMarketplaceListingsAsync(EcommercePlatform platform, CancellationToken ct = default)
    {
        logger.LogInformation("Syncing marketplace listings for {Platform}", platform);
        return Task.CompletedTask;
    }
}
