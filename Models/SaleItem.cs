using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

public class SaleItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Item-level discount rate (0–100 percentage).</summary>
    [Range(0, 100)]
    public decimal ItemDiscountRate { get; set; }

    /// <summary>Flat item-level discount amount (₹).</summary>
    public decimal ItemFlatDiscount { get; set; }

    /// <summary>Tax rate applied at time of sale (GST %).</summary>
    public decimal TaxRate { get; set; }

    /// <summary>Whether the sale price was tax-inclusive at time of sale.</summary>
    public bool IsTaxInclusive { get; set; }

    /// <summary>Computed tax amount for this line.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Cess rate applied at time of sale (%). (#197)</summary>
    public decimal CessRate { get; set; }

    /// <summary>Computed cess amount for this line. (#197)</summary>
    public decimal CessAmount { get; set; }

    /// <summary>CGST component (intra-state = TaxAmount / 2).</summary>
    [NotMapped]
    public decimal CgstAmount => TaxAmount / 2m;

    /// <summary>SGST component (intra-state = TaxAmount / 2).</summary>
    [NotMapped]
    public decimal SgstAmount => TaxAmount / 2m;

    /// <summary>IGST component (inter-state = TaxAmount). Used when buyer state differs from seller state.</summary>
    [NotMapped]
    public decimal IgstAmount => TaxAmount;

    /// <summary>CGST rate (half of GST rate for intra-state).</summary>
    [NotMapped]
    public decimal CgstRate => TaxRate / 2m;

    /// <summary>SGST rate (half of GST rate for intra-state).</summary>
    [NotMapped]
    public decimal SgstRate => TaxRate / 2m;

    /// <summary>Computed discount amount per unit.</summary>
    [NotMapped]
    public decimal ItemDiscountAmount => UnitPrice * ItemDiscountRate / 100m;

    /// <summary>Price after item discount.</summary>
    [NotMapped]
    public decimal DiscountedUnitPrice => UnitPrice - ItemDiscountAmount;

    [NotMapped]
    public decimal Subtotal => Quantity * DiscountedUnitPrice;

    /// <summary>Staff who sold this item (for per-item incentive tracking).</summary>
    public int? StaffId { get; set; }
    public Staff? Staff { get; set; }

    public int SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>
    /// Optional variant FK — tracks which variant was sold so returns
    /// can restore stock to the correct entity.
    /// </summary>
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
}
