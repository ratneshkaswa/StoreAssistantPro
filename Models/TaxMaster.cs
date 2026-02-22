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

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>Profiles that include this tax component.</summary>
    public ICollection<TaxProfileItem> ProfileItems { get; set; } = [];
}
