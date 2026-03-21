using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// A parked/held billing cart that can be recalled later (#336-#346).
/// Persisted to DB so it survives app restart (#345).
/// </summary>
public class HeldBill
{
    public int Id { get; set; }

    /// <summary>Display label (e.g., "Bill #1" or customer tag).</summary>
    [MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    /// <summary>Optional customer name tag for identification (#341).</summary>
    [MaxLength(100)]
    public string? CustomerTag { get; set; }

    /// <summary>Optional notes for context (#342).</summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Who held the bill.</summary>
    [MaxLength(20)]
    public string? CashierRole { get; set; }

    /// <summary>When the bill was held (IST).</summary>
    public DateTime HeldAt { get; set; }

    /// <summary>Snapshot total at time of hold.</summary>
    public decimal Total { get; set; }

    /// <summary>Number of items in cart at time of hold.</summary>
    public int ItemCount { get; set; }

    /// <summary>False after recall or stale cleanup (#346).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Cart line items.</summary>
    public List<HeldBillItem> Items { get; set; } = [];
}

/// <summary>
/// Single cart line item in a held bill.
/// </summary>
public class HeldBillItem
{
    public int Id { get; set; }

    public int HeldBillId { get; set; }
    public HeldBill HeldBill { get; set; } = null!;

    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }

    [MaxLength(300)]
    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsTaxInclusive { get; set; }
    public decimal ItemDiscountRate { get; set; }
    public decimal ItemDiscountAmount { get; set; }
    public decimal CessRate { get; set; }
}
