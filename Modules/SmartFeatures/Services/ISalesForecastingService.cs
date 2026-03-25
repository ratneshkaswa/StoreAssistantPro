using StoreAssistantPro.Models.AI;

namespace StoreAssistantPro.Modules.SmartFeatures.Services;

/// <summary>
/// Sales trend prediction and seasonal demand forecasting.
/// Features #519–522: trend prediction, seasonal forecasting,
/// auto reorder suggestions, stock optimization.
/// </summary>
public interface ISalesForecastingService
{
    /// <summary>Predict sales for the next N days based on historical data. (#519)</summary>
    Task<IReadOnlyList<SalesForecast>> ForecastSalesAsync(
        int daysAhead = 30, CancellationToken ct = default);

    /// <summary>Identify seasonal patterns (Diwali, wedding season) for stock planning. (#520)</summary>
    Task<IReadOnlyList<SalesForecast>> GetSeasonalForecastAsync(
        int monthsAhead = 3, CancellationToken ct = default);

    /// <summary>Suggest products to reorder based on sales velocity and lead time. (#521)</summary>
    Task<IReadOnlyList<ReorderSuggestion>> GetReorderSuggestionsAsync(
        int leadTimeDays = 7, CancellationToken ct = default);

    /// <summary>Recommend optimal stock levels to minimize carrying cost. (#522)</summary>
    Task<IReadOnlyList<StockOptimizationResult>> OptimizeStockLevelsAsync(
        CancellationToken ct = default);
}
