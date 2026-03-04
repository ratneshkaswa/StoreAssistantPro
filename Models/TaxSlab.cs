using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Price-based GST slab within a <see cref="TaxGroup"/>.
/// Indian GST for garments uses price-based slabs:
///   - Up to ₹1000: GST 5% (CGST 2.5% + SGST 2.5%)
///   - Above ₹1000: GST 12% (CGST 6% + SGST 6%)
/// Each slab auto-calculates CGST/SGST/IGST from the GSTPercent.
/// </summary>
public class TaxSlab
{
    public int Id { get; set; }

    public int TaxGroupId { get; set; }
    public TaxGroup? TaxGroup { get; set; }

    /// <summary>Composite GST rate (e.g., 5, 12, 18, 28).</summary>
    [Range(0, 100)]
    public decimal GSTPercent { get; set; }

    /// <summary>Auto-calculated: GSTPercent / 2 (intra-state).</summary>
    [Range(0, 50)]
    public decimal CGSTPercent { get; set; }

    /// <summary>Auto-calculated: GSTPercent / 2 (intra-state).</summary>
    [Range(0, 50)]
    public decimal SGSTPercent { get; set; }

    /// <summary>Auto-calculated: GSTPercent (inter-state).</summary>
    [Range(0, 100)]
    public decimal IGSTPercent { get; set; }

    /// <summary>
    /// Lower price boundary (inclusive) for this slab.
    /// Use 0 for the lowest slab.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal PriceFrom { get; set; }

    /// <summary>
    /// Upper price boundary (inclusive) for this slab.
    /// Use <see cref="decimal.MaxValue"/> for "and above".
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal PriceTo { get; set; } = decimal.MaxValue;

    /// <summary>Date from which this slab is effective.</summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    /// Date until which this slab is effective.
    /// Null means currently in effect (no end date).
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }
}
