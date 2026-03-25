namespace StoreAssistantPro.Models.Hardware;

/// <summary>Data for a single barcode label to be printed.</summary>
public sealed class LabelData
{
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public string? SizeText { get; set; }
    public string? ColorText { get; set; }
    public string? SKU { get; set; }
    public string? StoreName { get; set; }
}

/// <summary>Label print job containing one or more labels.</summary>
public sealed class LabelPrintJob
{
    public IReadOnlyList<LabelData> Labels { get; set; } = [];

    /// <summary>Paper size: "65up", "24up", "single", "custom".</summary>
    public string PaperSize { get; set; } = "65up";

    /// <summary>Label width in mm.</summary>
    public double LabelWidthMm { get; set; } = 38.1;

    /// <summary>Label height in mm.</summary>
    public double LabelHeightMm { get; set; } = 21.2;
}
