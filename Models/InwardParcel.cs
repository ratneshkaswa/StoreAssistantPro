using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Represents a single parcel within an <see cref="InwardEntry"/>.
/// Each parcel has its own inward number (e.g., 02-01, 02-02),
/// a category (from Products), and a vendor.
/// </summary>
public class InwardParcel
{
    public int Id { get; set; }

    public int InwardEntryId { get; set; }
    public InwardEntry? InwardEntry { get; set; }

    /// <summary>
    /// Individual parcel number within the month: MM-NN.
    /// Example: 02-01, 02-02 for the 1st and 2nd parcels in February.
    /// </summary>
    [Required, MaxLength(10)]
    public string ParcelNumber { get; set; } = string.Empty;

    /// <summary>
    /// Product category for this parcel.
    /// </summary>
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>
    /// Vendor who sent this parcel.
    /// </summary>
    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    [MaxLength(300)]
    public string? Description { get; set; }
}
