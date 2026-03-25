using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Models.Hardware;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Label printer service — generates ZPL (Zebra) and TSPL (TSC)
/// command strings for barcode labels, and sends them to the printer.
/// </summary>
public sealed class LabelPrinterService(
    ILogger<LabelPrinterService> logger) : ILabelPrinterService
{
    private HardwareDeviceConfig? _config;

    public DeviceConnectionStatus Status { get; private set; } = DeviceConnectionStatus.Disconnected;

    public Task<bool> ConnectAsync(HardwareDeviceConfig config, CancellationToken ct = default)
    {
        _config = config;
        Status = DeviceConnectionStatus.Connected;
        logger.LogInformation("Label printer connected: {Name} via {Type}",
            config.DeviceName, config.ConnectionType);
        return Task.FromResult(true);
    }

    public string GenerateZpl(LabelData label, double widthMm, double heightMm)
    {
        // ZPL II commands for Zebra printers.
        // 8 dots/mm is standard resolution for most Zebra models.
        var widthDots = (int)(widthMm * 8);
        var heightDots = (int)(heightMm * 8);

        var sb = new StringBuilder();
        sb.AppendLine("^XA");                                         // Start format
        sb.AppendLine(CultureInfo.InvariantCulture, $"^PW{widthDots}");                            // Print width
        sb.AppendLine(CultureInfo.InvariantCulture, $"^LL{heightDots}");                           // Label length

        // Store name
        if (!string.IsNullOrWhiteSpace(label.StoreName))
        {
            sb.AppendLine("^FO10,10^A0N,18,18");
            sb.AppendLine(CultureInfo.InvariantCulture, $"^FD{label.StoreName}^FS");
        }

        // Product name
        sb.AppendLine("^FO10,35^A0N,22,22");
        sb.AppendLine(CultureInfo.InvariantCulture, $"^FD{label.ProductName}^FS");

        // Size / Color
        var sizeColor = string.Join(" ", new[] { label.SizeText, label.ColorText }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(sizeColor))
        {
            sb.AppendLine("^FO10,60^A0N,18,18");
            sb.AppendLine(CultureInfo.InvariantCulture, $"^FD{sizeColor}^FS");
        }

        // Price
        sb.AppendLine("^FO10,85^A0N,24,24");
        sb.AppendLine(CultureInfo.InvariantCulture, $"^FD₹{label.Price:F2}^FS");

        // Barcode (EAN-13 / Code 128)
        if (!string.IsNullOrWhiteSpace(label.Barcode))
        {
            sb.AppendLine("^FO10,115^BY2");
            sb.AppendLine(CultureInfo.InvariantCulture, $"^BCN,40,Y,N,N^FD{label.Barcode}^FS");  // Code 128
        }

        sb.AppendLine("^XZ");                                         // End format
        return sb.ToString();
    }

    public string GenerateTspl(LabelData label, double widthMm, double heightMm)
    {
        // TSPL commands for TSC printers.
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"SIZE {widthMm:F1} mm, {heightMm:F1} mm");
        sb.AppendLine("GAP 3 mm, 0 mm");
        sb.AppendLine("DIRECTION 1");
        sb.AppendLine("CLS");

        // Store name
        if (!string.IsNullOrWhiteSpace(label.StoreName))
            sb.AppendLine(CultureInfo.InvariantCulture, $"TEXT 10,10,\"2\",0,1,1,\"{label.StoreName}\"");

        // Product name
        sb.AppendLine(CultureInfo.InvariantCulture, $"TEXT 10,35,\"3\",0,1,1,\"{label.ProductName}\"");

        // Size / Color
        var sizeColor = string.Join(" ", new[] { label.SizeText, label.ColorText }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(sizeColor))
            sb.AppendLine(CultureInfo.InvariantCulture, $"TEXT 10,60,\"2\",0,1,1,\"{sizeColor}\"");

        // Price
        sb.AppendLine(CultureInfo.InvariantCulture, $"TEXT 10,85,\"4\",0,1,1,\"Rs.{label.Price:F2}\"");

        // Barcode
        if (!string.IsNullOrWhiteSpace(label.Barcode))
            sb.AppendLine(CultureInfo.InvariantCulture, $"BARCODE 10,115,\"128\",40,1,0,2,2,\"{label.Barcode}\"");

        sb.AppendLine("PRINT 1,1");
        return sb.ToString();
    }

    public Task<bool> PrintBatchAsync(LabelPrintJob job, CancellationToken ct = default)
    {
        if (_config is null)
        {
            logger.LogWarning("Cannot print — no label printer configured");
            return Task.FromResult(false);
        }

        logger.LogInformation("Printing batch of {Count} labels on {Paper} paper",
            job.Labels.Count, job.PaperSize);

        // Generate commands per label based on printer type.
        var isZebra = _config.ModelName?.Contains("Zebra", StringComparison.OrdinalIgnoreCase) == true;

        foreach (var label in job.Labels)
        {
            var commands = isZebra
                ? GenerateZpl(label, job.LabelWidthMm, job.LabelHeightMm)
                : GenerateTspl(label, job.LabelWidthMm, job.LabelHeightMm);

            // Real implementation: send to printer port.
            logger.LogDebug("Label generated for {Product}: {Len} chars",
                label.ProductName, commands.Length);
        }

        return Task.FromResult(true);
    }

    public Task<byte[]> GeneratePreviewAsync(LabelData label, double widthMm, double heightMm, CancellationToken ct = default)
    {
        // Real implementation: render label to bitmap using WPF DrawingVisual
        // and return PNG bytes. For now, return empty preview.
        logger.LogDebug("Generating label preview for {Product}", label.ProductName);
        return Task.FromResult(Array.Empty<byte>());
    }

    public Task<bool> ConfigureAsync(HardwareDeviceConfig config, CancellationToken ct = default)
    {
        _config = config;
        logger.LogInformation("Label printer configured: {Name}, model {Model}",
            config.DeviceName, config.ModelName);
        return Task.FromResult(true);
    }

    public Task<bool> PrintWirelessAsync(LabelPrintJob job, string deviceAddress, CancellationToken ct = default)
    {
        // Real implementation: connect via Bluetooth or WiFi to mobile printer.
        logger.LogInformation("Printing {Count} labels wirelessly to {Address}",
            job.Labels.Count, deviceAddress);
        return PrintBatchAsync(job, ct);
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        Status = DeviceConnectionStatus.Disconnected;
        logger.LogInformation("Label printer disconnected");
        return Task.CompletedTask;
    }
}
