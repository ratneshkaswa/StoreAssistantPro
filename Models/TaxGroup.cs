using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Groups related tax slabs together for Indian GST.
/// Example: "GST Garments" contains price-based slabs (5% up to ₹1000, 12% above).
/// </summary>
public class TaxGroup
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>Price-based slabs belonging to this group.</summary>
    public ICollection<TaxSlab> Slabs { get; set; } = [];

    /// <summary>Products mapped to this group.</summary>
    public ICollection<ProductTaxMapping> ProductMappings { get; set; } = [];
}
