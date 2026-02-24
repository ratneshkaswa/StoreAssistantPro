using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

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
    /// When <c>false</c>, the product is hidden from billing and stock operations.
    /// </summary>
    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
