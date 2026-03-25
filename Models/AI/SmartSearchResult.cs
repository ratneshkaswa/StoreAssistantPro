namespace StoreAssistantPro.Models.AI;

/// <summary>A fuzzy/phonetic product search result with relevance score.</summary>
public sealed class SmartSearchResult
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public decimal SalePrice { get; init; }
    public int Quantity { get; init; }

    /// <summary>Relevance score 0.0–1.0 (1.0 = exact match).</summary>
    public double RelevanceScore { get; init; }

    /// <summary>Match reason: "Exact", "Fuzzy", "Phonetic", "Recent", "Frequent".</summary>
    public string MatchType { get; init; } = "Exact";
}
