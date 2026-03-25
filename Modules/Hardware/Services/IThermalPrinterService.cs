using StoreAssistantPro.Models.Hardware;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Manages thermal receipt printers via ESC/POS protocol.
/// Features #479–486: auto-detect, ESC/POS, logo, barcode on receipt,
/// cash drawer kick, paper cut, status monitoring, dual printer.
/// </summary>
public interface IThermalPrinterService
{
    /// <summary>Current printer status flags.</summary>
    PrinterStatusFlags Status { get; }

    /// <summary>Auto-detect connected thermal printer model. (#479)</summary>
    Task<HardwareDeviceConfig?> AutoDetectAsync(CancellationToken ct = default);

    /// <summary>Print a full receipt using ESC/POS commands. (#480)</summary>
    Task<bool> PrintReceiptAsync(ReceiptData receipt, CancellationToken ct = default);

    /// <summary>Print store logo bitmap on receipt. (#481)</summary>
    Task<bool> PrintLogoAsync(string logoPath, CancellationToken ct = default);

    /// <summary>Print barcode of invoice number on receipt. (#482)</summary>
    Task<bool> PrintBarcodeAsync(string barcodeData, CancellationToken ct = default);

    /// <summary>Send ESC/POS cash drawer kick command. (#483)</summary>
    Task<bool> KickCashDrawerAsync(CancellationToken ct = default);

    /// <summary>Send auto-cut command after receipt. (#484)</summary>
    Task<bool> CutPaperAsync(CancellationToken ct = default);

    /// <summary>Check printer online/offline/paper-out status. (#485)</summary>
    Task<PrinterStatusFlags> GetStatusAsync(CancellationToken ct = default);

    /// <summary>Configure printer connection. (#486 — part of dual printer support)</summary>
    Task<bool> ConfigureAsync(HardwareDeviceConfig config, CancellationToken ct = default);

    /// <summary>Open raw ESC/POS channel for custom commands.</summary>
    Task<bool> SendRawAsync(byte[] escPosData, CancellationToken ct = default);
}
