using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.AI;

namespace StoreAssistantPro.Modules.SmartFeatures.Services;

/// <summary>
/// Customer behavioral analysis — purchase patterns (market basket),
/// churn prediction, segmentation, and next-purchase prediction.
/// Uses SQL-based RFM analysis and co-occurrence counting.
/// </summary>
public sealed class CustomerInsightsService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<CustomerInsightsService> logger) : ICustomerInsightsService
{
    public async Task<IReadOnlyList<ProductAssociation>> GetProductAssociationsAsync(
        int minSupport = 5, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = DateTime.UtcNow.AddDays(-180);

        // Get all sale → product pairs for multi-item transactions.
        var saleProducts = await context.SaleItems
            .Where(si => si.Sale!.SaleDate >= cutoff)
            .GroupBy(si => si.SaleId)
            .Where(g => g.Count() >= 2) // Only multi-item sales
            .SelectMany(g => g.Select(si => new { si.SaleId, si.ProductId, si.Product!.Name }))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var totalTransactions = saleProducts.Select(sp => sp.SaleId).Distinct().Count();
        if (totalTransactions == 0) return [];

        // Build co-occurrence pairs.
        var salesGrouped = saleProducts.GroupBy(sp => sp.SaleId).ToList();
        var pairCounts = new Dictionary<(int, int), int>();
        var productNames = new Dictionary<int, string>();

        foreach (var sale in salesGrouped)
        {
            var items = sale.DistinctBy(s => s.ProductId).ToList();
            foreach (var item in items)
                productNames.TryAdd(item.ProductId, item.Name);

            for (var i = 0; i < items.Count; i++)
            {
                for (var j = i + 1; j < items.Count; j++)
                {
                    var key = items[i].ProductId < items[j].ProductId
                        ? (items[i].ProductId, items[j].ProductId)
                        : (items[j].ProductId, items[i].ProductId);

                    pairCounts[key] = pairCounts.GetValueOrDefault(key) + 1;
                }
            }
        }

        var associations = pairCounts
            .Where(kv => kv.Value >= minSupport)
            .Select(kv => new ProductAssociation
            {
                ProductAId = kv.Key.Item1,
                ProductAName = productNames.GetValueOrDefault(kv.Key.Item1, ""),
                ProductBId = kv.Key.Item2,
                ProductBName = productNames.GetValueOrDefault(kv.Key.Item2, ""),
                CoOccurrenceCount = kv.Value,
                SupportRatio = Math.Round((double)kv.Value / totalTransactions, 4),
                ConfidenceAtoB = Math.Round((double)kv.Value /
                    salesGrouped.Count(s => s.Any(x => x.ProductId == kv.Key.Item1)), 4)
            })
            .OrderByDescending(a => a.CoOccurrenceCount)
            .ToList();

        logger.LogInformation("Found {Count} product associations (min support={Min})",
            associations.Count, minSupport);
        return associations;
    }

    public async Task<IReadOnlyList<CustomerSegment>> GetChurnRiskCustomersAsync(
        double churnThreshold = 0.7, CancellationToken ct = default)
    {
        var segments = await SegmentCustomersAsync(ct).ConfigureAwait(false);
        return segments.Where(s => s.ChurnProbability >= churnThreshold).ToList();
    }

    public async Task<IReadOnlyList<CustomerSegment>> SegmentCustomersAsync(
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = DateTime.UtcNow.AddDays(-365);

        // RFM analysis: Recency, Frequency, Monetary.
        var customerMetrics = await context.Sales
            .Where(s => s.SaleDate >= cutoff && s.CustomerId != null)
            .GroupBy(s => new { s.CustomerId, s.Customer!.Name })
            .Select(g => new
            {
                CustomerId = g.Key.CustomerId!.Value,
                CustomerName = g.Key.Name,
                LastPurchase = g.Max(s => s.SaleDate),
                TransactionCount = g.Count(),
                TotalSpend = g.Sum(s => s.TotalAmount)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (customerMetrics.Count == 0) return [];

        var now = DateTime.UtcNow;
        var maxSpend = customerMetrics.Max(c => c.TotalSpend);
        var maxFreq = customerMetrics.Max(c => c.TransactionCount);

        var segments = customerMetrics.Select(c =>
        {
            var daysSinceLast = (int)(now - c.LastPurchase).TotalDays;
            var spendScore = maxSpend > 0 ? (double)(c.TotalSpend / maxSpend) : 0;
            var freqScore = maxFreq > 0 ? (double)c.TransactionCount / maxFreq : 0;
            var recencyScore = Math.Max(0, 1.0 - daysSinceLast / 365.0);

            // Churn probability: high recency gap + declining frequency.
            var churnProb = Math.Round(Math.Max(0, Math.Min(1.0,
                1.0 - recencyScore * 0.5 - freqScore * 0.3 - spendScore * 0.2)), 2);

            // Segment assignment.
            var segment = (spendScore, freqScore, recencyScore) switch
            {
                ( > 0.7, > 0.5, > 0.5) => "HighValue",
                (_, > 0.6, > 0.5) => "Frequent",
                (_, _, < 0.2) => "Dormant",
                (_, _, < 0.4) => "AtRisk",
                _ => "Regular"
            };

            // New customers: less than 30 days old with < 3 transactions.
            if (daysSinceLast < 30 && c.TransactionCount <= 2)
                segment = "New";

            return new CustomerSegment
            {
                CustomerId = c.CustomerId,
                CustomerName = c.CustomerName,
                Segment = segment,
                TotalSpend = c.TotalSpend,
                TransactionCount = c.TransactionCount,
                DaysSinceLastPurchase = daysSinceLast,
                ChurnProbability = churnProb
            };
        }).ToList();

        logger.LogInformation("Segmented {Count} customers: {Groups}",
            segments.Count,
            string.Join(", ", segments.GroupBy(s => s.Segment)
                .Select(g => $"{g.Key}={g.Count()}")));

        return segments;
    }

    public async Task<(DateTime predictedDate, IReadOnlyList<int> likelyProductIds)> PredictNextPurchaseAsync(
        int customerId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Average inter-purchase interval.
        var saleDates = await context.Sales
            .Where(s => s.CustomerId == customerId)
            .OrderBy(s => s.SaleDate)
            .Select(s => s.SaleDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        DateTime predictedDate;
        if (saleDates.Count >= 2)
        {
            var intervals = saleDates
                .Zip(saleDates.Skip(1), (a, b) => (b - a).TotalDays)
                .ToList();
            var avgInterval = intervals.Average();
            predictedDate = saleDates.Last().AddDays(avgInterval);
        }
        else
        {
            predictedDate = DateTime.UtcNow.AddDays(30); // Default: 30 days.
        }

        // Most frequently purchased products by this customer.
        var topProducts = await context.SaleItems
            .Where(si => si.Sale!.CustomerId == customerId)
            .GroupBy(si => si.ProductId)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (predictedDate, topProducts);
    }
}
