using StoreAssistantPro.Models.Hardware;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Manages USB/Bluetooth barcode scanner lifecycle.
/// Features #471–478: detection, continuous scan, routing, multi-scanner,
/// sound feedback, error handling, config, wireless.
/// </summary>
public interface IBarcodeScannerService
{
    /// <summary>Current connection status of the primary scanner.</summary>
    DeviceConnectionStatus Status { get; }

    /// <summary>All configured scanners and their statuses.</summary>
    IReadOnlyList<DeviceStatus> ConnectedScanners { get; }

    /// <summary>Detect and connect to USB HID barcode scanners. (#471)</summary>
    Task<bool> DetectAndConnectAsync(CancellationToken ct = default);

    /// <summary>Enable continuous scan mode — scanner stays active for rapid multi-item scanning. (#472)</summary>
    Task SetContinuousScanModeAsync(bool enabled, CancellationToken ct = default);

    /// <summary>Whether continuous scan mode is active.</summary>
    bool IsContinuousScanMode { get; }

    /// <summary>Route scanned input to a specific target field or billing search. (#473)</summary>
    Task SetScanRoutingAsync(string targetFieldName, CancellationToken ct = default);

    /// <summary>Configure a scanner device. (#477)</summary>
    Task<bool> ConfigureDeviceAsync(HardwareDeviceConfig config, CancellationToken ct = default);

    /// <summary>Pair a wireless (Bluetooth) scanner. (#478)</summary>
    Task<bool> PairWirelessScannerAsync(string deviceAddress, CancellationToken ct = default);

    /// <summary>Disconnect and release all scanners.</summary>
    Task DisconnectAllAsync(CancellationToken ct = default);

    /// <summary>Enable/disable scan sound feedback. (#475)</summary>
    bool SoundFeedbackEnabled { get; set; }

    /// <summary>
    /// Raised when a barcode is scanned.
    /// The event bus also receives <see cref="Events.BarcodeScanEvent"/>.
    /// </summary>
    event Action<BarcodeScanResult>? BarcodeScanned;
}
