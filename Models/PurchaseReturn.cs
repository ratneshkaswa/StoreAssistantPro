using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Tracks a return of goods back to a supplier (#374).
/// Reduces the outstanding payable to the supplier.
/// </summary>
public class PurchaseReturn
{
    public int Id { get; set; }

    /// <summary>Human-readable return reference number (e.g., PR-20250601-0001).</summary>
    [Required, MaxLength(30)]
    public string ReturnNumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }
    public Vendor? Supplier { get; set; }

    public DateTime ReturnDate { get; set; }

    /// <summary>Optional debit note number issued to the supplier (#375).</summary>
    [MaxLength(30)]
    public string? DebitNoteNumber { get; set; }

    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public ICollection<PurchaseReturnItem> Items { get; set; } = [];
}

/// <summary>
/// Line item in a purchase return.
/// </summary>
public class PurchaseReturnItem
{
    public int Id { get; set; }

    public int PurchaseReturnId { get; set; }
    public PurchaseReturn? PurchaseReturn { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }

    [MaxLength(200)]
    public string? Reason { get; set; }
}
