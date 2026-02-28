using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Vendor
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    [MaxLength(15)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    /// <summary>City for vendor location lookup and reporting.</summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>State code (e.g., "KA", "MH") for GST place-of-supply rules.</summary>
    [MaxLength(50)]
    public string? State { get; set; }

    /// <summary>PIN code for delivery/dispatch routing.</summary>
    [MaxLength(10)]
    public string? PinCode { get; set; }

    /// <summary>
    /// GST Identification Number (15-char alphanumeric for India).
    /// </summary>
    [MaxLength(15)]
    public string? GSTIN { get; set; }

    /// <summary>
    /// Payment terms (e.g., "Net 30", "Net 60", "COD", "Advance").
    /// </summary>
    [MaxLength(50)]
    public string? PaymentTerms { get; set; }

    /// <summary>
    /// Maximum credit limit allowed for this vendor (₹). 0 = no limit.
    /// </summary>
    public decimal CreditLimit { get; set; }

    /// <summary>Opening balance carried forward from previous system (₹).</summary>
    public decimal OpeningBalance { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Computed row highlight: <see cref="RowHighlightLevel.Inactive"/> when deactivated.</summary>
    public RowHighlightLevel HighlightLevel =>
        IsActive ? RowHighlightLevel.None : RowHighlightLevel.Inactive;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
