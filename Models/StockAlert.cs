using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Configurable stock alert rule for a product.
/// Triggers notifications when stock falls below or exceeds thresholds.
/// </summary>
public class StockAlert
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Low stock threshold. Alert when Quantity &lt;= this.</summary>
    public int LowThreshold { get; set; }

    /// <summary>Overstock threshold. Alert when Quantity &gt; this. 0 = no overstock alert.</summary>
    public int HighThreshold { get; set; }

    /// <summary>Whether this alert is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>When the alert was last triggered (for dedup).</summary>
    public DateTime? LastTriggeredDate { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
