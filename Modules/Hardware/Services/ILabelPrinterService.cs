using StoreAssistantPro.Models.Hardware;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Manages dedicated label printers (Zebra, TSC, etc.).
/// Features #503–511: dedicated printer, ZPL, TSPL, template designer,
/// auto-feed, batch printing, preview, config, wireless.
/// </summary>
public interface ILabelPrinterService
{
    /// <summary>Current connection status.</summary>
    DeviceConnectionStatus Status { get; }

    /// <summary>Connect to a label printer. (#503)</summary>
    Task<bool> ConnectAsync(HardwareDeviceConfig config, CancellationToken ct = default);

    /// <summary>Generate Zebra ZPL commands for a label. (#504)</summary>
    string GenerateZpl(LabelData label, double widthMm, double heightMm);

    /// <summary>Generate TSC TSPL commands for a label. (#505)</summary>
    string GenerateTspl(LabelData label, double widthMm, double heightMm);

    /// <summary>Print a batch of labels. (#508)</summary>
    Task<bool> PrintBatchAsync(LabelPrintJob job, CancellationToken ct = default);

    /// <summary>Generate a preview image of a label (PNG bytes). (#509)</summary>
    Task<byte[]> GeneratePreviewAsync(LabelData label, double widthMm, double heightMm, CancellationToken ct = default);

    /// <summary>Configure label printer model, port, dimensions. (#510)</summary>
    Task<bool> ConfigureAsync(HardwareDeviceConfig config, CancellationToken ct = default);

    /// <summary>Print labels over WiFi/Bluetooth to a mobile printer. (#511)</summary>
    Task<bool> PrintWirelessAsync(LabelPrintJob job, string deviceAddress, CancellationToken ct = default);

    /// <summary>Disconnect from the label printer.</summary>
    Task DisconnectAsync(CancellationToken ct = default);
}
