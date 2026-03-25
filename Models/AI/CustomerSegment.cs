namespace StoreAssistantPro.Models.AI;

/// <summary>Customer behavioral segment identified by analysis.</summary>
public sealed class CustomerSegment
{
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>Segment: "HighValue", "Frequent", "AtRisk", "Dormant", "New".</summary>
    public string Segment { get; init; } = string.Empty;

    /// <summary>Total spend in the analysis period.</summary>
    public decimal TotalSpend { get; init; }

    /// <summary>Number of transactions in the analysis period.</summary>
    public int TransactionCount { get; init; }

    /// <summary>Days since last purchase.</summary>
    public int DaysSinceLastPurchase { get; init; }

    /// <summary>Churn probability 0.0–1.0.</summary>
    public double ChurnProbability { get; init; }
}
