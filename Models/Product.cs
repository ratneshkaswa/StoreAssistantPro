using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal SalePrice { get; set; }

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

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
