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
}
