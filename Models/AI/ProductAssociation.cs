namespace StoreAssistantPro.Models.AI;

/// <summary>Products commonly purchased together (market basket).</summary>
public sealed class ProductAssociation
{
    public int ProductAId { get; init; }
    public string ProductAName { get; init; } = string.Empty;
    public int ProductBId { get; init; }
    public string ProductBName { get; init; } = string.Empty;

    /// <summary>How often A and B appear in the same transaction (0.0–1.0).</summary>
    public double SupportRatio { get; init; }

    /// <summary>Given A was purchased, probability of B also being purchased.</summary>
    public double ConfidenceAtoB { get; init; }

    /// <summary>Number of co-occurrence transactions.</summary>
    public int CoOccurrenceCount { get; init; }
}
