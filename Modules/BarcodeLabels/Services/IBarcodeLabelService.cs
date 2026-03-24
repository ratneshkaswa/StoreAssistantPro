using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.BarcodeLabels.Services;

/// <summary>
/// Service for barcode label generation and printing (#376-#380, #382, #445).
/// </summary>
public interface IBarcodeLabelService
{
    /// <summary>Returns products available for label printing (active, with barcode).</summary>
    Task<IReadOnlyList<BarcodeLabelProduct>> GetProductsForLabelAsync(CancellationToken ct = default);

    /// <summary>Returns firm info for label header.</summary>
    Task<string> GetFirmNameAsync(CancellationToken ct = default);

    /// <summary>Returns variants for a product, for printing variant-specific labels (#381).</summary>
    Task<IReadOnlyList<BarcodeLabelVariant>> GetVariantsForProductAsync(int productId, CancellationToken ct = default);

    /// <summary>Auto-generates an EAN-13 barcode for a product that has none (#384).</summary>
    Task<string> AutoGenerateBarcodeAsync(int productId, CancellationToken ct = default);

    /// <summary>Returns price tag data for shelf display labels (#446).</summary>
    Task<IReadOnlyList<PriceTagData>> GetPriceTagDataAsync(IReadOnlyList<int> productIds, CancellationToken ct = default);

    /// <summary>Returns the configured barcode format (#385) and label paper size (#386).</summary>
    Task<BarcodeLabelConfig> GetLabelConfigAsync(CancellationToken ct = default);
}

/// <summary>Product data needed for barcode label rendering.</summary>
public record BarcodeLabelProduct(
    int Id,
    string Name,
    string? Barcode,
    decimal SalePrice,
    decimal CostPrice,
    string? SKU,
    string? CategoryName,
    string? BrandName);

/// <summary>Variant data for size/color labels (#381).</summary>
public record BarcodeLabelVariant(
    int VariantId,
    int ProductId,
    string ProductName,
    string? SizeName,
    string? ColorName,
    string? Barcode,
    decimal AdditionalPrice);

/// <summary>Price tag data for shelf display labels (#446).</summary>
public record PriceTagData(
    int ProductId,
    string ProductName,
    decimal SalePrice,
    decimal CostPrice,
    string? Barcode,
    string? CategoryName,
    string? BrandName,
    string? SizeName,
    string? ColorName);

/// <summary>Barcode label configuration (#385/#386).</summary>
public record BarcodeLabelConfig(
    string BarcodeFormat,
    string LabelPaperSize);
