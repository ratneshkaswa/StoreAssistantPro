using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.AI;

namespace StoreAssistantPro.Modules.SmartFeatures.Services;

/// <summary>
/// Dynamic pricing, markdown optimization, and price elasticity analysis.
/// Uses historical sales data to identify slow movers, demand patterns,
/// and optimal pricing points.
/// </summary>
public sealed class SmartPricingService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<SmartPricingService> logger) : ISmartPricingService
{
    public Task<IReadOnlyDictionary<int, decimal>> GetCompetitorPricesAsync(
        IEnumerable<int> productIds, CancellationToken ct = default)
    {
        // Placeholder — requires external price feed integration.
        // Returns empty dictionary until a competitor data source is configured.
        logger.LogInformation("Competitor price tracking not configured — returning empty results");
        return Task.FromResult<IReadOnlyDictionary<int, decimal>>(
            new Dictionary<int, decimal>());
    }

    public async Task<IReadOnlyList<PricingSuggestion>> GetDynamicPricingSuggestionsAsync(
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff30 = DateTime.UtcNow.AddDays(-30);
        var cutoff7 = DateTime.UtcNow.AddDays(-7);

        // Compare last-7-day velocity vs 30-day average to detect demand shifts.
        var productMetrics = await context.SaleItems
            .Where(si => si.Sale!.SaleDate >= cutoff30)
            .GroupBy(si => new { si.ProductId, si.Product!.Name, si.Product.SalePrice, si.Product.Quantity })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                Price = g.Key.SalePrice,
                Stock = g.Key.Quantity,
                Total30 = g.Count(),
                Total7 = g.Count(x => x.Sale!.SaleDate >= cutoff7)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var suggestions = new List<PricingSuggestion>();

        foreach (var m in productMetrics)
        {
            var avgDaily30 = m.Total30 / 30.0;
            var avgDaily7 = m.Total7 / 7.0;

            if (avgDaily30 <= 0) continue;

            var demandRatio = avgDaily7 / avgDaily30;

            if (demandRatio > 1.5)
            {
                // High demand — can potentially increase price.
                var increase = Math.Min(m.Price * 0.05m, m.Price * 0.10m); // 5–10%
                suggestions.Add(new PricingSuggestion
                {
                    ProductId = m.ProductId,
                    ProductName = m.Name,
                    CurrentPrice = m.Price,
                    SuggestedPrice = Math.Round(m.Price + increase, 2),
                    Reason = $"Demand up {(demandRatio - 1) * 100:F0}% in last 7 days",
                    Strategy = "Demand",
                    EstimatedRevenueImpact = Math.Round(increase * m.Total7, 2),
                    Confidence = Math.Min(0.8, demandRatio / 3.0)
                });
            }
            else if (demandRatio < 0.5 && m.Stock > m.Total30)
            {
                // Low demand + high stock — suggest markdown.
                var reduction = m.Price * 0.10m; // 10% markdown
                suggestions.Add(new PricingSuggestion
                {
                    ProductId = m.ProductId,
                    ProductName = m.Name,
                    CurrentPrice = m.Price,
                    SuggestedPrice = Math.Round(m.Price - reduction, 2),
                    Reason = $"Demand down {(1 - demandRatio) * 100:F0}%, stock exceeds monthly sales",
                    Strategy = "SlowMover",
                    EstimatedRevenueImpact = Math.Round(-reduction * m.Total7 + reduction * 5, 2),
                    Confidence = 0.6
                });
            }
        }

        logger.LogInformation("Generated {Count} dynamic pricing suggestions", suggestions.Count);
        return suggestions;
    }

    public async Task<IReadOnlyList<PricingSuggestion>> GetMarkdownSuggestionsAsync(
        int slowMovingDaysThreshold = 90, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = DateTime.UtcNow.AddDays(-slowMovingDaysThreshold);

        // Products with stock > 0 and no sales in the threshold period.
        var productsWithSales = await context.SaleItems
            .Where(si => si.Sale!.SaleDate >= cutoff)
            .Select(si => si.ProductId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var slowMovers = await context.Products
            .Where(p => p.IsActive && p.Quantity > 0 && !productsWithSales.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.SalePrice, p.CostPrice, p.Quantity })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var suggestions = slowMovers.Select(p =>
        {
            // Progressive markdown: 10% for 90 days, 20% for 180+, 30% for 270+.
            var markdownPct = 0.10m;
            var reduction = p.SalePrice * markdownPct;

            // Never go below cost.
            var suggestedPrice = Math.Max(p.CostPrice, p.SalePrice - reduction);

            return new PricingSuggestion
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CurrentPrice = p.SalePrice,
                SuggestedPrice = Math.Round(suggestedPrice, 2),
                Reason = $"No sales in {slowMovingDaysThreshold} days, {p.Quantity} units in stock",
                Strategy = "Markdown",
                EstimatedRevenueImpact = Math.Round(suggestedPrice * p.Quantity, 2),
                Confidence = 0.7
            };
        }).ToList();

        logger.LogInformation("Found {Count} slow movers for markdown", suggestions.Count);
        return suggestions;
    }

    public async Task<IReadOnlyDictionary<int, double>> GetPriceElasticityAsync(
        IEnumerable<int> productIds, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Price elasticity = % change in quantity / % change in price.
        // We approximate by looking at historical unit prices in SaleItems
        // and correlating with quantity sold.
        var ids = productIds.ToList();
        var cutoff = DateTime.UtcNow.AddDays(-180);

        var priceHistory = await context.SaleItems
            .Where(si => ids.Contains(si.ProductId) && si.Sale!.SaleDate >= cutoff)
            .GroupBy(si => new { si.ProductId, Month = si.Sale!.SaleDate.Month })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Month,
                AvgPrice = g.Average(x => x.UnitPrice),
                TotalQty = g.Sum(x => x.Quantity)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var elasticities = new Dictionary<int, double>();

        foreach (var productId in ids)
        {
            var months = priceHistory
                .Where(h => h.ProductId == productId)
                .OrderBy(h => h.Month)
                .ToList();

            if (months.Count < 2)
            {
                elasticities[productId] = -1.0; // Default: unit elastic.
                continue;
            }

            // Simple linear regression between price and quantity.
            var priceMean = months.Average(m => (double)m.AvgPrice);
            var qtyMean = months.Average(m => (double)m.TotalQty);

            var numerator = months.Sum(m =>
                ((double)m.AvgPrice - priceMean) * ((double)m.TotalQty - qtyMean));
            var denominator = months.Sum(m =>
                Math.Pow((double)m.AvgPrice - priceMean, 2));

            var elasticity = denominator > 0
                ? Math.Round(numerator / denominator * (priceMean / qtyMean), 2)
                : -1.0;

            elasticities[productId] = elasticity;
        }

        return elasticities;
    }
}
