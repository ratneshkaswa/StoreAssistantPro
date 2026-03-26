using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.Ecommerce;

namespace StoreAssistantPro.Modules.Ecommerce.Events;

/// <summary>Published when an online order is imported.</summary>
public sealed class OnlineOrderImportedEvent(OnlineOrder order) : IEvent
{
    public OnlineOrder Order { get; } = order;
}

/// <summary>Published when a platform connection status changes.</summary>
public sealed class PlatformConnectionChangedEvent(EcommercePlatform platform, bool isConnected) : IEvent
{
    public EcommercePlatform Platform { get; } = platform;
    public bool IsConnected { get; } = isConnected;
}

/// <summary>Published when product listings are synced to a platform.</summary>
public sealed class ProductListingSyncedEvent(EcommercePlatform platform, int productCount) : IEvent
{
    public EcommercePlatform Platform { get; } = platform;
    public int ProductCount { get; } = productCount;
}
