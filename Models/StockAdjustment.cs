using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Predefined reasons for stock quantity adjustments.
/// </summary>
public enum AdjustmentReason
{
    Damage,
    Theft,
    Correction,
    Return,
    Transfer,
    OpeningStock,
    Other
}

/// <summary>
/// Immutable audit record for every stock quantity change.
/// </summary>
public class StockAdjustment
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Variant FK — null means a product-level (non-variant) adjustment.</summary>
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }

    /// <summary>Signed delta: NewQuantity − OldQuantity.</summary>
    public int QuantityChange => NewQuantity - OldQuantity;

    public AdjustmentReason Reason { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }

    public int UserId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
