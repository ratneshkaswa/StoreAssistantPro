using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Simple tax slab for Indian GST.
/// Examples: GST 0% → 0, GST 5% → 5, GST 12% → 12, GST 18% → 18.
/// TaxName must be unique.
/// </summary>
public class TaxMaster
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string TaxName { get; set; } = string.Empty;

    /// <summary>Tax slab percentage (0–100).</summary>
    [Range(0, 100)]
    public decimal SlabPercent { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
