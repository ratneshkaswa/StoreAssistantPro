using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Many-to-many join: a product can have multiple suppliers,
/// each with their own cost price (#92, #93).
/// </summary>
public class ProductSupplier
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Supplier-specific cost price for this product (#93).</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Supplier's own product code / SKU for ordering.</summary>
    [MaxLength(50)]
    public string? SupplierSKU { get; set; }

    /// <summary>Whether this is the preferred/primary supplier for the product.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Lead time in days for delivery from this supplier.</summary>
    public int LeadTimeDays { get; set; }

    /// <summary>Minimum order quantity from this supplier.</summary>
    public int MinOrderQty { get; set; } = 1;
}
