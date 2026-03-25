namespace StoreAssistantPro.Models.AI;

/// <summary>Dynamic pricing suggestion based on demand/stock/competition.</summary>
public sealed class PricingSuggestion
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal CurrentPrice { get; init; }
    public decimal SuggestedPrice { get; init; }
    public string Reason { get; init; } = string.Empty;

    /// <summary>Pricing strategy: "Markdown", "Demand", "Competitive", "SlowMover".</summary>
    public string Strategy { get; init; } = string.Empty;

    /// <summary>Expected revenue impact of applying this price change.</summary>
    public decimal EstimatedRevenueImpact { get; init; }

    /// <summary>Confidence 0.0–1.0.</summary>
    public double Confidence { get; init; }
}
