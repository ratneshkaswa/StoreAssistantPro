using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Represents a single inward receipt when parcels arrive at the shop.
/// Groups multiple parcels under one entry with shared transport charges.
/// </summary>
public class InwardEntry
{
    public int Id { get; set; }

    /// <summary>
    /// Auto-generated inward number: MM-NN where MM = month, NN = sequence within that month.
    /// Example: 02-01, 02-02 for February; 03-01 for March.
    /// </summary>
    [Required, MaxLength(10)]
    public string InwardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date of receipt. Defaults to today but can be changed manually.
    /// </summary>
    public DateTime InwardDate { get; set; }

    /// <summary>
    /// Total number of parcels received in this entry.
    /// </summary>
    public int ParcelCount { get; set; }

    /// <summary>
    /// Collective transport charges for all parcels in this entry.
    /// Reflected in Expenses.
    /// </summary>
    public decimal TransportCharges { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<InwardParcel> Parcels { get; set; } = [];
}
