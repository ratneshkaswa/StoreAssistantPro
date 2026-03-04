using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Master tax definition for Indian GST structure.
/// Supports CGST, SGST, IGST, and custom tax slabs (0%, 5%, 12%, 18%, 28%).
/// Only one record may be marked <see cref="IsDefault"/> at a time.
/// </summary>
public class TaxMaster
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string TaxName { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Harmonized System Nomenclature code for this tax slab.
    /// Indian GST uses 4–8 digit HSN codes for goods classification.
    /// </summary>
    [MaxLength(8)]
    public string? HSNCode { get; set; }

    /// <summary>
    /// Which product category this tax slab applies to.
    /// Defaults to <see cref="TaxApplicableCategory.Both"/>.
    /// </summary>
    public TaxApplicableCategory ApplicableCategory { get; set; } = TaxApplicableCategory.Both;

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>Profiles that include this tax component.</summary>
    public ICollection<TaxProfileItem> ProfileItems { get; set; } = [];
}
