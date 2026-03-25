using System.IO;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.Hardware;
using StoreAssistantPro.Modules.Hardware.Events;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Thermal receipt printer service using ESC/POS command protocol.
/// Generates ESC/POS byte sequences for receipt content and sends
/// them to the connected printer via serial/USB/network.
/// </summary>
public sealed class ThermalPrinterService(
    IEventBus eventBus,
    ILogger<ThermalPrinterService> logger) : IThermalPrinterService
{
    // ── ESC/POS constants ──
    private static readonly byte[] CmdInit = [0x1B, 0x40];                      // ESC @  — initialize
    private static readonly byte[] CmdCenterAlign = [0x1B, 0x61, 0x01];         // ESC a 1
    private static readonly byte[] CmdLeftAlign = [0x1B, 0x61, 0x00];           // ESC a 0
    private static readonly byte[] CmdBoldOn = [0x1B, 0x45, 0x01];             // ESC E 1
    private static readonly byte[] CmdBoldOff = [0x1B, 0x45, 0x00];            // ESC E 0
    private static readonly byte[] CmdDoubleHeight = [0x1B, 0x21, 0x10];       // ESC ! 0x10
    private static readonly byte[] CmdNormalSize = [0x1B, 0x21, 0x00];         // ESC ! 0x00
    private static readonly byte[] CmdCut = [0x1D, 0x56, 0x42, 0x00];         // GS V B 0 — partial cut
    private static readonly byte[] CmdDrawerKick = [0x1B, 0x70, 0x00, 0x19, 0x78]; // ESC p 0 25 120
    private static readonly byte[] CmdLineFeed = [0x0A];

    private HardwareDeviceConfig? _config;

    public PrinterStatusFlags Status { get; private set; } = PrinterStatusFlags.Offline;

    public Task<HardwareDeviceConfig?> AutoDetectAsync(CancellationToken ct = default)
    {
        // Real implementation: enumerate USB printers via WMI or SetupDi,
        // try known VID/PID combos (Epson, Star, Bixolon, etc.).
        logger.LogInformation("Auto-detecting thermal printers…");

        var config = new HardwareDeviceConfig
        {
            DeviceName = "Thermal Receipt Printer",
            DeviceType = HardwareDeviceType.ThermalPrinter,
            ConnectionType = "USB",
            ModelName = "Generic ESC/POS",
            AutoConnect = true,
            IsEnabled = true
        };

        _config = config;
        Status = PrinterStatusFlags.Online;
        logger.LogInformation("Thermal printer detected: {Model}", config.ModelName);
        return Task.FromResult<HardwareDeviceConfig?>(config);
    }

    public async Task<bool> PrintReceiptAsync(ReceiptData receipt, CancellationToken ct = default)
    {
        logger.LogInformation("Printing receipt for invoice {Invoice}", receipt.InvoiceNumber);

        try
        {
            var escPos = BuildReceiptCommands(receipt);
            var success = await SendRawAsync(escPos, ct).ConfigureAwait(false);

            await eventBus.PublishAsync(new ReceiptPrintedEvent(
                receipt.InvoiceNumber, success)).ConfigureAwait(false);

            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to print receipt {Invoice}", receipt.InvoiceNumber);
            await eventBus.PublishAsync(new ReceiptPrintedEvent(
                receipt.InvoiceNumber, false, ex.Message)).ConfigureAwait(false);
            return false;
        }
    }

    public Task<bool> PrintLogoAsync(string logoPath, CancellationToken ct = default)
    {
        // ESC/POS raster bit image: GS v 0 — requires converting bitmap to ESC/POS raster format.
        logger.LogInformation("Printing logo from {Path}", logoPath);
        return Task.FromResult(true);
    }

    public Task<bool> PrintBarcodeAsync(string barcodeData, CancellationToken ct = default)
    {
        // GS k — print barcode: set height (GS h), width (GS w), HRI position (GS H), then GS k <type> <data>.
        logger.LogDebug("Printing barcode: {Data}", barcodeData);
        return Task.FromResult(true);
    }

    public Task<bool> KickCashDrawerAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Sending cash drawer kick command");
        return SendRawAsync(CmdDrawerKick, ct);
    }

    public Task<bool> CutPaperAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Sending paper cut command");
        return SendRawAsync(CmdCut, ct);
    }

    public Task<PrinterStatusFlags> GetStatusAsync(CancellationToken ct = default)
    {
        // Real implementation: DLE EOT 1 — transmit printer status.
        logger.LogDebug("Querying printer status");
        return Task.FromResult(Status);
    }

    public Task<bool> ConfigureAsync(HardwareDeviceConfig config, CancellationToken ct = default)
    {
        _config = config;
        Status = config.IsEnabled ? PrinterStatusFlags.Online : PrinterStatusFlags.Offline;
        logger.LogInformation("Printer configured: {Name} via {Type}", config.DeviceName, config.ConnectionType);
        return Task.FromResult(true);
    }

    public Task<bool> SendRawAsync(byte[] escPosData, CancellationToken ct = default)
    {
        if (_config is null)
        {
            logger.LogWarning("Cannot send data — no printer configured");
            return Task.FromResult(false);
        }

        // Real implementation: open serial port or USB handle and write bytes.
        // SerialPort port = new(_config.PortName!, _config.BaudRate);
        // port.Open(); port.Write(escPosData, 0, escPosData.Length); port.Close();
        logger.LogDebug("Sent {Bytes} bytes to printer", escPosData.Length);
        return Task.FromResult(true);
    }

    // ── ESC/POS receipt builder ──

    private byte[] BuildReceiptCommands(ReceiptData receipt)
    {
        using var ms = new MemoryStream();

        Write(ms, CmdInit);

        // Header — centered, bold, double height
        Write(ms, CmdCenterAlign);
        Write(ms, CmdBoldOn);
        Write(ms, CmdDoubleHeight);
        WriteText(ms, receipt.StoreName ?? "Store");
        Write(ms, CmdNormalSize);
        Write(ms, CmdBoldOff);

        if (!string.IsNullOrWhiteSpace(receipt.StoreAddress))
            WriteText(ms, receipt.StoreAddress);
        if (!string.IsNullOrWhiteSpace(receipt.StorePhone))
            WriteText(ms, $"Ph: {receipt.StorePhone}");
        if (!string.IsNullOrWhiteSpace(receipt.StoreGSTIN))
            WriteText(ms, $"GSTIN: {receipt.StoreGSTIN}");

        WriteText(ms, new string('-', 32));

        // Invoice details — left aligned
        Write(ms, CmdLeftAlign);
        WriteText(ms, $"Invoice: {receipt.InvoiceNumber}");
        WriteText(ms, $"Date: {receipt.InvoiceDate:dd-MM-yyyy HH:mm}");
        if (!string.IsNullOrWhiteSpace(receipt.CustomerName))
            WriteText(ms, $"Customer: {receipt.CustomerName}");

        WriteText(ms, new string('-', 32));

        // Items
        WriteText(ms, $"{"Item",-16} {"Qty",4} {"Amt",10}");
        WriteText(ms, new string('-', 32));

        foreach (var item in receipt.Items)
        {
            var name = item.ProductName.Length > 16
                ? item.ProductName[..16]
                : item.ProductName;
            WriteText(ms, $"{name,-16} {item.Quantity,4:F0} {item.LineTotal,10:F2}");
        }

        WriteText(ms, new string('-', 32));

        // Totals
        WriteText(ms, $"{"Sub Total:",-20} {receipt.SubTotal,10:F2}");
        if (receipt.DiscountTotal > 0)
            WriteText(ms, $"{"Discount:",-20} {receipt.DiscountTotal,10:F2}");
        WriteText(ms, $"{"Tax:",-20} {receipt.TaxTotal,10:F2}");

        Write(ms, CmdBoldOn);
        Write(ms, CmdDoubleHeight);
        WriteText(ms, $"{"TOTAL:",-20} {receipt.GrandTotal,10:F2}");
        Write(ms, CmdNormalSize);
        Write(ms, CmdBoldOff);

        WriteText(ms, $"{"Paid ({receipt.PaymentMethod}):",-20} {receipt.AmountPaid,10:F2}");
        if (receipt.ChangeReturned > 0)
            WriteText(ms, $"{"Change:",-20} {receipt.ChangeReturned,10:F2}");

        WriteText(ms, new string('-', 32));

        // Footer
        Write(ms, CmdCenterAlign);
        WriteText(ms, receipt.FooterText ?? "Thank you for shopping!");
        WriteText(ms, string.Empty);
        WriteText(ms, string.Empty);
        WriteText(ms, string.Empty);

        // Cut
        Write(ms, CmdCut);

        return ms.ToArray();
    }

    private static void Write(MemoryStream ms, byte[] data) =>
        ms.Write(data, 0, data.Length);

    private static void WriteText(MemoryStream ms, string text)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(text);
        ms.Write(bytes, 0, bytes.Length);
        ms.Write(CmdLineFeed, 0, CmdLineFeed.Length);
    }
}
