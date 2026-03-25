using Microsoft.Extensions.Logging;
using StoreAssistantPro.Models.Hardware;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Customer-facing display service — drives VFD pole displays via
/// ESC/POS, LCD panels, and second-monitor screens.
/// </summary>
public sealed class CustomerDisplayService(
    ILogger<CustomerDisplayService> logger) : ICustomerDisplayService
{
    // ESC/POS VFD commands (common for Epson DM-D / Star SCD / Birch BCD)
    private static readonly byte[] VfdInit = [0x1B, 0x40];             // ESC @
    private static readonly byte[] VfdClear = [0x0C];                  // FF — clear display
    private static readonly byte[] VfdLine1 = [0x1B, 0x51, 0x41];     // ESC Q A — move to upper line
    private static readonly byte[] VfdLine2 = [0x1B, 0x51, 0x42];     // ESC Q B — move to lower line

    private HardwareDeviceConfig? _poleConfig;
    private HardwareDeviceConfig? _lcdConfig;

    public DeviceConnectionStatus Status { get; private set; } = DeviceConnectionStatus.Disconnected;

    public Task<bool> ConnectPoleDisplayAsync(HardwareDeviceConfig config, CancellationToken ct = default)
    {
        _poleConfig = config;
        Status = DeviceConnectionStatus.Connected;
        logger.LogInformation("VFD pole display connected on {Port}", config.PortName);
        return Task.FromResult(true);
    }

    public Task<bool> ConnectLcdDisplayAsync(HardwareDeviceConfig config, CancellationToken ct = default)
    {
        _lcdConfig = config;
        Status = DeviceConnectionStatus.Connected;
        logger.LogInformation("LCD customer display connected on {Port}", config.PortName);
        return Task.FromResult(true);
    }

    public Task ShowItemAsync(string productName, decimal price, CancellationToken ct = default)
    {
        // Truncate product name to 20 chars (standard VFD width).
        var name = productName.Length > 20 ? productName[..20] : productName.PadRight(20);
        var priceText = $"Rs.{price:F2}".PadLeft(20);

        logger.LogDebug("Display: {Name} | {Price}", name, priceText);

        // Real: send ESC Q A + name bytes + ESC Q B + price bytes to serial port.
        return Task.CompletedTask;
    }

    public Task ShowTotalAsync(decimal grandTotal, CancellationToken ct = default)
    {
        var line1 = "   GRAND TOTAL      ";
        var line2 = $"Rs.{grandTotal:F2}".PadLeft(20);

        logger.LogDebug("Display total: {Line1} | {Line2}", line1.Trim(), line2.Trim());
        return Task.CompletedTask;
    }

    public Task ShowIdleMessageAsync(string message, CancellationToken ct = default)
    {
        var line1 = message.Length > 20 ? message[..20] : message.PadRight(20);
        var line2 = "  Welcome!          ";

        logger.LogDebug("Display idle: {Line1} | {Line2}", line1.Trim(), line2.Trim());
        return Task.CompletedTask;
    }

    public Task UpdateSecondScreenAsync(CustomerDisplayContent content, CancellationToken ct = default)
    {
        // Real implementation: send content to a WPF window on secondary monitor,
        // or via a named-pipe / WebSocket to a customer-facing display app.
        logger.LogDebug("Second screen update: {Line1} | {Line2}", content.Line1, content.Line2);
        return Task.CompletedTask;
    }

    public Task ShowPromotionalContentAsync(string contentPath, CancellationToken ct = default)
    {
        // Real implementation: display image/video on second monitor during idle.
        logger.LogDebug("Showing promotional content from {Path}", contentPath);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Clearing customer display");
        return Task.CompletedTask;
    }

    public Task DisconnectAllAsync(CancellationToken ct = default)
    {
        _poleConfig = null;
        _lcdConfig = null;
        Status = DeviceConnectionStatus.Disconnected;
        logger.LogInformation("All customer displays disconnected");
        return Task.CompletedTask;
    }
}
