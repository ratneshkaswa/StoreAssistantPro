using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Represents a single parcel within an <see cref="InwardEntry"/>.
/// Each parcel has its own parcel number (e.g., 02-01, 02-02),
/// a vendor, and up to 3 product line items.
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
    /// Vendor who sent this parcel.
    /// </summary>
    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    /// <summary>
    /// Transport charge allocated to this individual parcel.
    /// </summary>
    public decimal TransportCharge { get; set; }

    [MaxLength(300)]
    public string? Description { get; set; }

    /// <summary>
    /// Product line items in this parcel (maximum 3 per parcel).
    /// </summary>
    public ICollection<InwardProduct> Products { get; set; } = [];
}
