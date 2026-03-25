namespace StoreAssistantPro.Models.AI;

/// <summary>A recommendation to reorder a specific product.</summary>
public sealed class ReorderSuggestion
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int MinStockLevel { get; init; }
    public int SuggestedQuantity { get; init; }

    /// <summary>Average daily sales velocity over the lookback period.</summary>
    public decimal DailySalesVelocity { get; init; }

    /// <summary>Estimated days until stockout at current velocity.</summary>
    public int EstimatedDaysToStockout { get; init; }

    /// <summary>Priority: High (< 3 days), Medium (3–7 days), Low (> 7 days).</summary>
    public ReorderPriority Priority { get; init; }
}

public enum ReorderPriority
{
    Low,
    Medium,
    High,
    Critical
}
