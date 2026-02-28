using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Immutable audit record for every stock quantity change.
/// Created by <c>AdjustStockHandler</c> alongside the actual quantity update.
/// <para>
/// <b>Feature #68</b> — Stock adjustment log / audit trail.
/// </para>
/// </summary>
public class StockAdjustmentLog
{
    public int Id { get; set; }

    /// <summary>Product whose stock was adjusted.</summary>
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Product name snapshot at time of adjustment (denormalized for audit).</summary>
    [Required, MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Stock quantity before the adjustment.</summary>
    public int QuantityBefore { get; set; }

    /// <summary>Signed adjustment amount (+N or −N).</summary>
    public int AdjustmentQty { get; set; }

    /// <summary>Stock quantity after the adjustment (<see cref="QuantityBefore"/> + <see cref="AdjustmentQty"/>).</summary>
    public int QuantityAfter { get; set; }

    /// <summary>Free-text reason provided by the user (optional).</summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>IST timestamp when the adjustment was recorded.</summary>
    public DateTime AdjustedAt { get; set; }

    /// <summary>
    /// Source of the adjustment for filtering/reporting.
    /// </summary>
    [MaxLength(50)]
    public string Source { get; set; } = "Manual";
}
