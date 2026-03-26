using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.Mobile;

namespace StoreAssistantPro.Modules.Mobile.Events;

/// <summary>Published when a mobile device registers for push notifications.</summary>
public sealed class MobileDeviceRegisteredEvent(string platform, string deviceToken) : IEvent
{
    public string Platform { get; } = platform;
    public string DeviceToken { get; } = deviceToken;
}

/// <summary>Published when mobile offline data is synced.</summary>
public sealed class MobileSyncCompletedEvent(MobileSyncState state) : IEvent
{
    public MobileSyncState State { get; } = state;
}

/// <summary>Published when a mobile quick sale is processed.</summary>
public sealed class MobileQuickSaleEvent(int saleId) : IEvent
{
    public int SaleId { get; } = saleId;
}
