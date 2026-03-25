namespace StoreAssistantPro.Models.Hardware;

/// <summary>Result of a barcode scan event.</summary>
public sealed class BarcodeScanResult
{
    /// <summary>Raw scanned string from the scanner.</summary>
    public required string RawBarcode { get; init; }

    /// <summary>Cleaned / normalized barcode value.</summary>
    public string Barcode => RawBarcode.Trim();

    /// <summary>Timestamp of the scan.</summary>
    public DateTime ScannedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Which scanner produced this scan (device name).</summary>
    public string? SourceDevice { get; init; }
}
