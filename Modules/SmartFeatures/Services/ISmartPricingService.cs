using StoreAssistantPro.Models.AI;

namespace StoreAssistantPro.Modules.SmartFeatures.Services;

/// <summary>
/// Dynamic pricing and markdown optimization.
/// Features #523–526: competitor price tracking, dynamic pricing rules,
/// markdown optimization, price elasticity analysis.
/// </summary>
public interface ISmartPricingService
{
    /// <summary>Track competitor prices for key products. (#523) — placeholder for external feed.</summary>
    Task<IReadOnlyDictionary<int, decimal>> GetCompetitorPricesAsync(
        IEnumerable<int> productIds, CancellationToken ct = default);

    /// <summary>Auto-adjust prices based on demand, stock level, and competition. (#524)</summary>
    Task<IReadOnlyList<PricingSuggestion>> GetDynamicPricingSuggestionsAsync(
        CancellationToken ct = default);

    /// <summary>Suggest optimal markdown timing and percentage for slow movers. (#525)</summary>
    Task<IReadOnlyList<PricingSuggestion>> GetMarkdownSuggestionsAsync(
        int slowMovingDaysThreshold = 90, CancellationToken ct = default);

    /// <summary>Measure how price changes affect sales volume. (#526)</summary>
    Task<IReadOnlyDictionary<int, double>> GetPriceElasticityAsync(
        IEnumerable<int> productIds, CancellationToken ct = default);
}
