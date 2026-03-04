using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Harmonized System Nomenclature code master for Indian GST.
/// HSN codes are required on tax invoices for goods classification.
/// 4-digit for turnover up to ₹5 Cr, 6-digit above.
/// </summary>
public class HSNCode
{
    public int Id { get; set; }

    /// <summary>4–8 digit HSN code (e.g., "6109" for T-shirts).</summary>
    [Required, MaxLength(8)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable description of the HSN classification.</summary>
    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Whether this HSN applies to Garments, Fabric, or Both.</summary>
    public HSNCategory Category { get; set; } = HSNCategory.Garments;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    /// <summary>Products mapped to this HSN code.</summary>
    public ICollection<ProductTaxMapping> ProductMappings { get; set; } = [];
}

/// <summary>
/// HSN code classification for Indian textile/clothing GST.
/// </summary>
public enum HSNCategory
{
    Garments,
    Fabric,
    Both
}
