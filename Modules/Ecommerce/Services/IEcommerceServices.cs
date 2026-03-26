using StoreAssistantPro.Models.Ecommerce;

namespace StoreAssistantPro.Modules.Ecommerce.Services;

/// <summary>E-commerce platform connection service (#640-653).</summary>
public interface IPlatformConnectionService
{
    Task<IReadOnlyList<PlatformConnection>> GetConnectionsAsync(CancellationToken ct = default);
    Task<PlatformConnection> SaveConnectionAsync(PlatformConnection connection, CancellationToken ct = default);
    Task DeleteConnectionAsync(int connectionId, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(int connectionId, CancellationToken ct = default);
}

/// <summary>Product sync to e-commerce platforms (#640-651).</summary>
public interface IProductSyncService
{
    Task<int> PushProductsAsync(EcommercePlatform platform, IReadOnlyList<int>? productIds = null, CancellationToken ct = default);
    Task SyncInventoryAsync(EcommercePlatform platform, CancellationToken ct = default);
    Task SyncPricesAsync(EcommercePlatform platform, CancellationToken ct = default);
    Task<IReadOnlyList<ProductListing>> GetListingsAsync(int productId, CancellationToken ct = default);
}

/// <summary>Online order management (#642, #654-663).</summary>
public interface IOnlineOrderService
{
    Task<IReadOnlyList<OnlineOrder>> PullOrdersAsync(EcommercePlatform platform, CancellationToken ct = default);
    Task<IReadOnlyList<OnlineOrder>> GetOrdersAsync(EcommercePlatform? platform = null, string? status = null, CancellationToken ct = default);
    Task<int> ConvertToSaleAsync(int onlineOrderId, CancellationToken ct = default);
    Task UpdateOrderStatusAsync(int orderId, string status, string? trackingNumber = null, CancellationToken ct = default);
    Task CancelOrderAsync(int orderId, string? reason = null, CancellationToken ct = default);
}

/// <summary>Marketplace management (#664-673).</summary>
public interface IMarketplaceService
{
    Task<IReadOnlyList<MarketplaceCommission>> GetCommissionsAsync(EcommercePlatform platform, DateTime from, DateTime to, CancellationToken ct = default);
    Task SyncMarketplaceListingsAsync(EcommercePlatform platform, CancellationToken ct = default);
}
