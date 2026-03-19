using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Tracks a returned item from a completed sale.
/// Linked to the original <see cref="Sale"/> and <see cref="SaleItem"/>.
/// </summary>
public class SaleReturn
{
    public int Id { get; set; }

    /// <summary>Human-readable return reference number.</summary>
    [Required, MaxLength(30)]
    public string ReturnNumber { get; set; } = string.Empty;

    /// <summary>Original sale being returned against.</summary>
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }

    /// <summary>Original sale item being returned.</summary>
    public int SaleItemId { get; set; }
    public SaleItem? SaleItem { get; set; }

    /// <summary>Quantity returned (may be partial return).</summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>Refund amount for this return line.</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>Credit note number for this return (e.g., CN-20250101-0001).</summary>
    [MaxLength(30)]
    public string CreditNoteNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Who processed the return.</summary>
    [MaxLength(20)]
    public string? ProcessedByRole { get; set; }

    /// <summary>Role of the approver (manager/admin) who authorized this return.</summary>
    [MaxLength(20)]
    public string? ApprovedByRole { get; set; }

    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

    /// <summary>Whether stock was restored for this return.</summary>
    public bool StockRestored { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
