using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Classification: readymade garment or garment cloth (fabric).
    /// Drives unit, tax applicability, and available attributes.
    /// </summary>
    public ProductType ProductType { get; set; } = ProductType.Readymade;

    /// <summary>
    /// Unit of sale: Piece for readymade, Meter for garment cloth.
    /// </summary>
    public ProductUnit Unit { get; set; } = ProductUnit.Piece;

    // ── Attribute flags (controls which attributes appear during inward/billing) ──

    /// <summary>Whether this product supports colour selection.</summary>
    public bool SupportsColour { get; set; } = true;

    /// <summary>Whether this product supports pattern selection.</summary>
    public bool SupportsPattern { get; set; }

    /// <summary>Whether this product supports size selection.</summary>
    public bool SupportsSize { get; set; } = true;

    /// <summary>Whether this product supports type selection (e.g., Half Sleeve, Full Sleeve).</summary>
    public bool SupportsType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Purchase / cost price used for margin calculation.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>
    /// Harmonized System Nomenclature code for Indian GST classification.
    /// 4–8 digit code required on tax invoices for goods (optional for services under ₹5L turnover).
    /// </summary>
    [MaxLength(8)]
    public string? HSNCode { get; set; }

    public int? TaxProfileId { get; set; }
    public TaxProfile? TaxProfile { get; set; }

    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }

    /// <summary>
    /// When <c>true</c>, <see cref="Price"/> includes tax and the tax amount
    /// must be back-calculated. When <c>false</c> (default), <see cref="Price"/>
    /// is the base price and tax is added on top.
    /// </summary>
    public bool IsTaxInclusive { get; set; }

    /// <summary>
    /// Product barcode (EAN-13, Code128, or custom). Unique per product.
    /// </summary>
    [MaxLength(50)]
    public string? Barcode { get; set; }

    /// <summary>
    /// Stock Keeping Unit — internal product code.
    /// </summary>
    [MaxLength(50)]
    public string? SKU { get; set; }

    /// <summary>
    /// Optional product category for grouping.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Unit of measurement (e.g., pcs, meters, sets). Defaults to "pcs".
    /// </summary>
    [MaxLength(20)]
    public string UOM { get; set; } = "pcs";

    /// <summary>
    /// Minimum stock level (reorder point). Low stock alerts trigger when Quantity falls below this.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MinStockLevel { get; set; }

    /// <summary>
    /// <c>true</c> when <see cref="Quantity"/> is at or below <see cref="MinStockLevel"/>
    /// and MinStockLevel is configured (greater than zero).
    /// </summary>
    public bool IsLowStock => MinStockLevel > 0 && Quantity <= MinStockLevel;

    /// <summary>
    /// Computed stock value at cost: <see cref="CostPrice"/> × <see cref="Quantity"/>.
    /// </summary>
    public decimal StockValue => CostPrice * Quantity;

    /// <summary>
    /// Computed margin per unit: <see cref="SalePrice"/> − <see cref="CostPrice"/>.
    /// </summary>
    public decimal Margin => SalePrice - CostPrice;

    /// <summary>
    /// Computed margin percentage: (SalePrice − CostPrice) / SalePrice × 100.
    /// Returns 0 when SalePrice is zero.
    /// </summary>
    public decimal MarginPercent => SalePrice > 0 ? Math.Round((SalePrice - CostPrice) / SalePrice * 100, 1) : 0;

    /// <summary>
    /// Computed retail value: <see cref="SalePrice"/> × <see cref="Quantity"/>.
    /// </summary>
    public decimal RetailValue => SalePrice * Quantity;

    /// <summary>
    /// <c>true</c> when <see cref="Quantity"/> exceeds <see cref="MaxStockLevel"/>
    /// and MaxStockLevel is configured (greater than zero).
    /// </summary>
    public bool IsOverStock => MaxStockLevel > 0 && Quantity > MaxStockLevel;

    /// <summary>
    /// Maximum stock level (overstock threshold). 0 means unlimited.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MaxStockLevel { get; set; }

    /// <summary>
    /// When <c>false</c>, the product is hidden from billing and stock operations.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Product color (e.g., Red, Blue, Multi). Essential for clothing retail.
    /// </summary>
    [MaxLength(50)]
    public string? Color { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Computed row highlight based on stock and active status.
    /// </summary>
    public RowHighlightLevel HighlightLevel =>
        !IsActive ? RowHighlightLevel.Inactive :
        MinStockLevel > 0 && Quantity == 0 ? RowHighlightLevel.Danger :
        MinStockLevel > 0 && Quantity <= MinStockLevel ? RowHighlightLevel.Warning :
        RowHighlightLevel.None;
}
