using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.AI;

namespace StoreAssistantPro.Modules.SmartFeatures.Services;

/// <summary>
/// Sales forecasting using historical sales data analysis.
/// Uses moving averages, seasonal decomposition, and velocity-based
/// reorder calculations — no external ML library required.
/// </summary>
public sealed class SalesForecastingService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<SalesForecastingService> logger) : ISalesForecastingService
{
    public async Task<IReadOnlyList<SalesForecast>> ForecastSalesAsync(
        int daysAhead = 30, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Gather daily sales totals for the last 90 days.
        var cutoff = DateTime.UtcNow.AddDays(-90);
        var dailySales = await context.Sales
            .Where(s => s.SaleDate >= cutoff)
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(s => s.TotalAmount) })
            .OrderBy(g => g.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (dailySales.Count == 0)
        {
            logger.LogInformation("No sales history for forecasting");
            return [];
        }

        // Simple moving average forecast.
        var avg = dailySales.Average(d => d.Total);
        var stdDev = dailySales.Count > 1
            ? (decimal)Math.Sqrt(dailySales.Average(d => (double)((d.Total - avg) * (d.Total - avg))))
            : 0m;

        // 7-day moving average for recent trend.
        var recentAvg = dailySales.TakeLast(7).Average(d => d.Total);

        // Detect weekly seasonality (day-of-week pattern).
        var dayOfWeekAvg = dailySales
            .GroupBy(d => d.Date.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Average(x => x.Total));

        var forecasts = new List<SalesForecast>();
        var today = DateTime.UtcNow.Date;

        for (var i = 1; i <= daysAhead; i++)
        {
            var forecastDate = today.AddDays(i);
            var dayOfWeek = forecastDate.DayOfWeek;

            // Blend: 60% recent trend + 40% day-of-week pattern.
            var dayFactor = dayOfWeekAvg.TryGetValue(dayOfWeek, out var dowAvg) ? dowAvg : avg;
            var predicted = recentAvg * 0.6m + dayFactor * 0.4m;

            forecasts.Add(new SalesForecast
            {
                PeriodStart = forecastDate,
                PeriodEnd = forecastDate.AddDays(1),
                PredictedAmount = Math.Round(predicted, 2),
                ConfidenceLow = Math.Round(predicted - stdDev, 2),
                ConfidenceHigh = Math.Round(predicted + stdDev, 2),
                Confidence = dailySales.Count >= 30 ? 0.75 : 0.5,
                IsSeasonalPeak = predicted > avg * 1.3m
            });
        }

        logger.LogInformation("Generated {Count}-day forecast from {HistoryDays} days of history",
            daysAhead, dailySales.Count);
        return forecasts;
    }

    public async Task<IReadOnlyList<SalesForecast>> GetSeasonalForecastAsync(
        int monthsAhead = 3, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Monthly aggregates for up to 2 years of history.
        var cutoff = DateTime.UtcNow.AddYears(-2);
        var monthlySales = await context.Sales
            .Where(s => s.SaleDate >= cutoff)
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(s => s.TotalAmount) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (monthlySales.Count == 0)
            return [];

        // Known Indian retail seasonal peaks.
        var seasonalTags = new Dictionary<int, string>
        {
            { 1, "New Year / Republic Day" },
            { 3, "Holi" },
            { 8, "Raksha Bandhan" },
            { 9, "Navratri" },
            { 10, "Dussehra / Diwali prep" },
            { 11, "Diwali / Wedding Season" },
            { 12, "Wedding Season / Christmas" }
        };

        var monthAvg = monthlySales
            .GroupBy(m => m.Month)
            .ToDictionary(g => g.Key, g => g.Average(x => x.Total));
        var overallAvg = monthlySales.Average(m => m.Total);

        var forecasts = new List<SalesForecast>();
        var today = DateTime.UtcNow;

        for (var i = 1; i <= monthsAhead; i++)
        {
            var forecastMonth = today.AddMonths(i);
            var month = forecastMonth.Month;
            var predicted = monthAvg.TryGetValue(month, out var mAvg) ? mAvg : overallAvg;
            var isPeak = predicted > overallAvg * 1.2m;

            forecasts.Add(new SalesForecast
            {
                PeriodStart = new DateTime(forecastMonth.Year, month, 1),
                PeriodEnd = new DateTime(forecastMonth.Year, month, 1).AddMonths(1).AddDays(-1),
                PredictedAmount = Math.Round(predicted, 2),
                ConfidenceLow = Math.Round(predicted * 0.8m, 2),
                ConfidenceHigh = Math.Round(predicted * 1.2m, 2),
                Confidence = monthlySales.Count >= 12 ? 0.7 : 0.4,
                IsSeasonalPeak = isPeak,
                SeasonTag = seasonalTags.GetValueOrDefault(month)
            });
        }

        return forecasts;
    }

    public async Task<IReadOnlyList<ReorderSuggestion>> GetReorderSuggestionsAsync(
        int leadTimeDays = 7, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = DateTime.UtcNow.AddDays(-30);

        // Daily sales velocity per product over last 30 days.
        var velocities = await context.SaleItems
            .Where(si => si.Sale!.SaleDate >= cutoff)
            .GroupBy(si => new { si.ProductId, si.Product!.Name, si.Product.Quantity, si.Product.MinStockLevel })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                CurrentStock = g.Key.Quantity,
                MinStock = g.Key.MinStockLevel,
                TotalSold = g.Sum(x => (int)x.Quantity)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var suggestions = new List<ReorderSuggestion>();

        foreach (var v in velocities)
        {
            var dailyVelocity = v.TotalSold / 30m;
            if (dailyVelocity <= 0) continue;

            var daysToStockout = dailyVelocity > 0
                ? (int)(v.CurrentStock / dailyVelocity)
                : int.MaxValue;

            // Only suggest reorder if stockout within 2× lead time.
            if (daysToStockout > leadTimeDays * 2) continue;

            var suggestedQty = (int)Math.Ceiling(dailyVelocity * leadTimeDays * 1.5m) - v.CurrentStock;
            if (suggestedQty <= 0) continue;

            var priority = daysToStockout switch
            {
                <= 0 => ReorderPriority.Critical,
                <= 3 => ReorderPriority.High,
                <= 7 => ReorderPriority.Medium,
                _ => ReorderPriority.Low
            };

            suggestions.Add(new ReorderSuggestion
            {
                ProductId = v.ProductId,
                ProductName = v.Name,
                CurrentStock = v.CurrentStock,
                MinStockLevel = v.MinStock,
                SuggestedQuantity = suggestedQty,
                DailySalesVelocity = Math.Round(dailyVelocity, 2),
                EstimatedDaysToStockout = daysToStockout,
                Priority = priority
            });
        }

        logger.LogInformation("Generated {Count} reorder suggestions", suggestions.Count);
        return suggestions.OrderBy(s => s.Priority).ToList();
    }

    public async Task<IReadOnlyList<StockOptimizationResult>> OptimizeStockLevelsAsync(
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = DateTime.UtcNow.AddDays(-90);

        var productData = await context.SaleItems
            .Where(si => si.Sale!.SaleDate >= cutoff)
            .GroupBy(si => new { si.ProductId, si.Product!.Name, si.Product.Quantity, si.Product.CostPrice })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                CurrentStock = g.Key.Quantity,
                CostPrice = g.Key.CostPrice,
                TotalSold = g.Sum(x => (int)x.Quantity),
                SaleDays = g.Select(x => x.Sale!.SaleDate.Date).Distinct().Count()
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var results = new List<StockOptimizationResult>();

        foreach (var p in productData)
        {
            var avgDailySales = p.SaleDays > 0 ? p.TotalSold / (decimal)p.SaleDays : 0;
            if (avgDailySales <= 0) continue;

            // Safety stock = 1.5× lead time demand. Assume 7-day lead time.
            var safetyStock = (int)Math.Ceiling(avgDailySales * 7 * 1.5m);
            var maxStock = (int)Math.Ceiling(avgDailySales * 30); // 1-month supply

            var excessStock = Math.Max(0, p.CurrentStock - maxStock);
            var savings = excessStock * p.CostPrice * 0.02m; // 2% carrying cost per month

            results.Add(new StockOptimizationResult
            {
                ProductId = p.ProductId,
                ProductName = p.Name,
                CurrentStock = p.CurrentStock,
                RecommendedMinStock = safetyStock,
                RecommendedMaxStock = maxStock,
                EstimatedCarryingCostPerUnit = Math.Round(p.CostPrice * 0.02m, 2),
                PotentialSavings = Math.Round(savings, 2)
            });
        }

        logger.LogInformation("Optimized stock levels for {Count} products", results.Count);
        return results.OrderByDescending(r => r.PotentialSavings).ToList();
    }
}
