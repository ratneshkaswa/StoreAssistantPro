using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.Hardware;
using StoreAssistantPro.Modules.Hardware.Events;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Barcode scanner service. USB HID scanners appear as keyboard input
/// devices — this service manages detection, routing, and event publishing.
/// Full hardware communication requires platform-specific USB HID libraries.
/// </summary>
public sealed class BarcodeScannerService(
    IEventBus eventBus,
    ILogger<BarcodeScannerService> logger) : IBarcodeScannerService, IDisposable
{
    private readonly List<DeviceStatus> _connectedScanners = [];
    private string _routeTarget = "BillingSearch";
    private bool _disposed;

    public DeviceConnectionStatus Status { get; private set; } = DeviceConnectionStatus.Disconnected;
    public IReadOnlyList<DeviceStatus> ConnectedScanners => _connectedScanners.AsReadOnly();
    public bool IsContinuousScanMode { get; private set; }
    public bool SoundFeedbackEnabled { get; set; } = true;

    public event Action<BarcodeScanResult>? BarcodeScanned;

    public Task<bool> DetectAndConnectAsync(CancellationToken ct = default)
    {
        // USB HID scanner detection — scanners present as keyboard devices.
        // Real implementation would enumerate HID devices via SetupDiGetClassDevs / HidD_GetAttributes.
        logger.LogInformation("Scanning for USB HID barcode scanners…");

        var scanner = new DeviceStatus
        {
            DeviceName = "Default USB Scanner",
            DeviceType = HardwareDeviceType.BarcodeScanner,
            ConnectionStatus = DeviceConnectionStatus.Connected,
            LastSeenUtc = DateTime.UtcNow
        };

        _connectedScanners.Add(scanner);
        Status = DeviceConnectionStatus.Connected;

        eventBus.PublishAsync(new DeviceStatusChangedEvent(scanner));
        logger.LogInformation("USB barcode scanner connected: {Name}", scanner.DeviceName);
        return Task.FromResult(true);
    }

    public Task SetContinuousScanModeAsync(bool enabled, CancellationToken ct = default)
    {
        IsContinuousScanMode = enabled;
        logger.LogInformation("Continuous scan mode {State}", enabled ? "enabled" : "disabled");
        return Task.CompletedTask;
    }

    public Task SetScanRoutingAsync(string targetFieldName, CancellationToken ct = default)
    {
        _routeTarget = targetFieldName;
        logger.LogDebug("Scan routing set to: {Target}", targetFieldName);
        return Task.CompletedTask;
    }

    public Task<bool> ConfigureDeviceAsync(HardwareDeviceConfig config, CancellationToken ct = default)
    {
        logger.LogInformation("Configuring scanner: {Name} on {Port}",
            config.DeviceName, config.PortName ?? config.ConnectionType);
        return Task.FromResult(true);
    }

    public Task<bool> PairWirelessScannerAsync(string deviceAddress, CancellationToken ct = default)
    {
        // Bluetooth pairing — requires Windows Bluetooth APIs.
        logger.LogInformation("Pairing wireless scanner at {Address}", deviceAddress);

        var scanner = new DeviceStatus
        {
            DeviceName = $"Bluetooth Scanner ({deviceAddress})",
            DeviceType = HardwareDeviceType.BarcodeScanner,
            ConnectionStatus = DeviceConnectionStatus.Connected,
            LastSeenUtc = DateTime.UtcNow
        };
        _connectedScanners.Add(scanner);
        return Task.FromResult(true);
    }

    public Task DisconnectAllAsync(CancellationToken ct = default)
    {
        foreach (var s in _connectedScanners)
            s.ConnectionStatus = DeviceConnectionStatus.Disconnected;

        _connectedScanners.Clear();
        Status = DeviceConnectionStatus.Disconnected;
        logger.LogInformation("All barcode scanners disconnected");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called externally (e.g., by a keyboard hook or WPF PreviewTextInput handler)
    /// when a barcode scan is detected from keyboard-wedge input.
    /// </summary>
    public void ProcessScan(string rawBarcode, string? sourceDevice = null)
    {
        if (_disposed || string.IsNullOrWhiteSpace(rawBarcode)) return;

        var result = new BarcodeScanResult
        {
            RawBarcode = rawBarcode,
            SourceDevice = sourceDevice ?? "Default USB Scanner"
        };

        logger.LogDebug("Barcode scanned: {Barcode} from {Source} → routing to {Target}",
            result.Barcode, result.SourceDevice, _routeTarget);

        BarcodeScanned?.Invoke(result);
        _ = eventBus.PublishAsync(new BarcodeScanEvent(result));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connectedScanners.Clear();
    }
}
