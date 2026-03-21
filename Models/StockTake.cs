using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

/// <summary>
/// A physical stock count session (#69).
/// Captures system vs physical quantities for reconciliation.
/// </summary>
public class StockTake
{
    public int Id { get; set; }

    /// <summary>Auto-generated reference like "ST-20250101-001".</summary>
    [MaxLength(50)]
    public string Reference { get; set; } = string.Empty;

    public StockTakeStatus Status { get; set; } = StockTakeStatus.InProgress;

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int UserId { get; set; }

    public int TotalItems { get; set; }
    public int DiscrepancyCount { get; set; }

    public List<StockTakeItem> Items { get; set; } = [];
}

/// <summary>
/// Single product line in a stock take.
/// </summary>
public class StockTakeItem
{
    public int Id { get; set; }

    public int StockTakeId { get; set; }
    public StockTake StockTake { get; set; } = null!;

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int? ProductVariantId { get; set; }

    [MaxLength(300)]
    public string ProductName { get; set; } = string.Empty;

    public int SystemQuantity { get; set; }
    public int? CountedQuantity { get; set; }

    /// <summary>Counted − System (0 if not yet counted).</summary>
    [NotMapped]
    public int Variance => (CountedQuantity ?? SystemQuantity) - SystemQuantity;

    public bool IsCounted { get; set; }
}

public enum StockTakeStatus
{
    InProgress,
    Completed,
    Cancelled
}
