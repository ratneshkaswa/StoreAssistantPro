namespace StoreAssistantPro.Models.AI;

/// <summary>A predicted sales figure for a specific date range.</summary>
public sealed class SalesForecast
{
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public decimal PredictedAmount { get; init; }
    public decimal ConfidenceLow { get; init; }
    public decimal ConfidenceHigh { get; init; }

    /// <summary>Confidence level 0.0–1.0.</summary>
    public double Confidence { get; init; }

    /// <summary>Whether this period is flagged as seasonal peak.</summary>
    public bool IsSeasonalPeak { get; init; }

    /// <summary>Season tag if applicable (e.g., "Diwali", "Wedding Season").</summary>
    public string? SeasonTag { get; init; }
}
