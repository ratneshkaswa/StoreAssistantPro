namespace StoreAssistantPro.Models.AI;

/// <summary>Optimal stock level recommendation for a product.</summary>
public sealed class StockOptimizationResult
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int RecommendedMinStock { get; init; }
    public int RecommendedMaxStock { get; init; }
    public decimal EstimatedCarryingCostPerUnit { get; init; }

    /// <summary>Potential savings if stock is optimized to recommended levels.</summary>
    public decimal PotentialSavings { get; init; }
}
