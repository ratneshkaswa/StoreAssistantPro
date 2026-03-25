using StoreAssistantPro.Models.Hardware;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Manages customer-facing displays (VFD pole, LCD, second monitor).
/// Features #512–518: pole display, LCD, item-by-item, total,
/// idle message, customer-facing screen, promotional content.
/// </summary>
public interface ICustomerDisplayService
{
    /// <summary>Current connection status.</summary>
    DeviceConnectionStatus Status { get; }

    /// <summary>Connect to a VFD pole display. (#512)</summary>
    Task<bool> ConnectPoleDisplayAsync(HardwareDeviceConfig config, CancellationToken ct = default);

    /// <summary>Connect to an LCD customer display. (#513)</summary>
    Task<bool> ConnectLcdDisplayAsync(HardwareDeviceConfig config, CancellationToken ct = default);

    /// <summary>Show an individual scanned item on the display. (#514)</summary>
    Task ShowItemAsync(string productName, decimal price, CancellationToken ct = default);

    /// <summary>Show the grand total prominently. (#515)</summary>
    Task ShowTotalAsync(decimal grandTotal, CancellationToken ct = default);

    /// <summary>Show store name or promo when no transaction active. (#516)</summary>
    Task ShowIdleMessageAsync(string message, CancellationToken ct = default);

    /// <summary>Push full cart content to a customer-facing second monitor. (#517)</summary>
    Task UpdateSecondScreenAsync(CustomerDisplayContent content, CancellationToken ct = default);

    /// <summary>Show promotional content on the customer-facing screen during idle. (#518)</summary>
    Task ShowPromotionalContentAsync(string contentPath, CancellationToken ct = default);

    /// <summary>Clear the display.</summary>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>Disconnect from all displays.</summary>
    Task DisconnectAllAsync(CancellationToken ct = default);
}
