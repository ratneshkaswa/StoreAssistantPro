using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.AI;
using StoreAssistantPro.Modules.SmartFeatures.Events;

namespace StoreAssistantPro.Modules.SmartFeatures.Services;

/// <summary>
/// Anomaly and fraud detection engine — identifies unusual transactions,
/// theft patterns, inventory shrinkage, price anomalies, discount abuse,
/// and void/cancel patterns using statistical analysis on sales data.
/// </summary>
public sealed class AnomalyDetectionService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regionalSettings,
    IEventBus eventBus,
    ILogger<AnomalyDetectionService> logger) : IAnomalyDetectionService
{
    public async Task<IReadOnlyList<AnomalyAlert>> DetectUnusualTransactionsAsync(
        int lookbackDays = 30, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = regionalSettings.Now.AddDays(-lookbackDays);

        // Get sale stats for baseline.
        var sales = await context.Sales
            .Where(s => s.SaleDate >= cutoff)
            .Select(s => new
            {
                s.Id,
                Total = s.TotalAmount,
                s.DiscountAmount,
                s.SaleDate,
                ItemCount = s.Items.Count
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (sales.Count < 10) return []; // Not enough data.

        var avgTotal = sales.Average(s => s.Total);
        var stdDevTotal = (decimal)Math.Sqrt(sales.Average(s =>
            (double)((s.Total - avgTotal) * (s.Total - avgTotal))));

        var alerts = new List<AnomalyAlert>();
        var threshold = avgTotal + 3 * stdDevTotal; // 3-sigma rule

        foreach (var sale in sales.Where(s => s.Total > threshold))
        {
            alerts.Add(new AnomalyAlert
            {
                AlertType = "UnusualTransaction",
                Severity = "High",
                Description = $"Sale #{sale.Id} total {regionalSettings.FormatCurrency(sale.Total)} exceeds 3σ threshold ({regionalSettings.FormatCurrency(threshold)})",
                RelatedEntityId = sale.Id,
                RelatedEntityType = "Sale",
                AnomalyScore = Math.Min(1.0, (double)(sale.Total / threshold))
            });
        }

        // Late-night transactions (after 10 PM or before 6 AM).
        foreach (var sale in sales.Where(s => s.SaleDate.Hour is >= 22 or < 6))
        {
            alerts.Add(new AnomalyAlert
            {
                AlertType = "UnusualTransaction",
                Severity = "Medium",
                Description = $"Sale #{sale.Id} at {regionalSettings.FormatTime(sale.SaleDate)} — outside normal business hours",
                RelatedEntityId = sale.Id,
                RelatedEntityType = "Sale",
                AnomalyScore = 0.6
            });
        }

        logger.LogInformation("Detected {Count} unusual transactions", alerts.Count);
        return alerts;
    }

    public async Task<IReadOnlyList<AnomalyAlert>> DetectTheftPatternsAsync(
        int lookbackDays = 30, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = regionalSettings.Now.AddDays(-lookbackDays);
        var alerts = new List<AnomalyAlert>();

        // Pattern: Staff with high discount ratios (sweethearting).
        var staffStats = await context.Sales
            .Where(s => s.SaleDate >= cutoff && s.StaffId != null)
            .GroupBy(s => s.StaffId)
            .Select(g => new
            {
                StaffId = g.Key,
                TotalSales = g.Count(),
                DiscountedCount = g.Count(s => s.DiscountAmount > 0),
                TotalDiscount = g.Sum(s => s.DiscountAmount),
                TotalRevenue = g.Sum(s => s.TotalAmount)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var staff in staffStats.Where(u => u.TotalSales > 5))
        {
            var discountedRate = (double)staff.DiscountedCount / staff.TotalSales;
            if (discountedRate > 0.5) // More than 50% of sales have discounts.
            {
                alerts.Add(new AnomalyAlert
                {
                    AlertType = "TheftPattern",
                    Severity = discountedRate > 0.75 ? "Critical" : "High",
                    Description = $"Staff {staff.StaffId}: {discountedRate:P0} of sales have discounts ({staff.DiscountedCount}/{staff.TotalSales})",
                    RelatedEntityId = staff.StaffId,
                    RelatedEntityType = "Staff",
                    AnomalyScore = Math.Min(1.0, discountedRate * 1.5)
                });
            }

            // Sweethearting: high discount ratio relative to revenue.
            if (staff.TotalRevenue > 0)
            {
                var discountRatio = (double)(staff.TotalDiscount / staff.TotalRevenue);
                if (discountRatio > 0.2) // More than 20% of revenue as discounts.
                {
                    alerts.Add(new AnomalyAlert
                    {
                        AlertType = "TheftPattern",
                        Severity = "High",
                        Description = $"Staff {staff.StaffId}: {discountRatio:P0} of revenue given as discount",
                        RelatedEntityId = staff.StaffId,
                        RelatedEntityType = "Staff",
                        AnomalyScore = Math.Min(1.0, discountRatio * 2)
                    });
                }
            }
        }

        logger.LogInformation("Detected {Count} theft patterns", alerts.Count);
        return alerts;
    }

    public async Task<IReadOnlyList<AnomalyAlert>> DetectInventoryShrinkageAsync(
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = regionalSettings.Now.AddDays(-30);
        var alerts = new List<AnomalyAlert>();

        // Compare expected stock (opening + purchases - sales) vs actual.
        var products = await context.Products
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                CurrentQty = p.Quantity,
                TotalSold = context.SaleItems
                    .Where(si => si.ProductId == p.Id && si.Sale!.SaleDate >= cutoff)
                    .Sum(si => (int?)si.Quantity) ?? 0,
                TotalReceived = context.InwardProducts
                    .Where(ip => ip.ProductId == p.Id && ip.InwardParcel!.InwardEntry!.InwardDate >= cutoff)
                    .Sum(ip => (int?)ip.Quantity) ?? 0
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var p in products)
        {
            // Shrinkage = stock is lower than what sales/purchases can explain.
            // Expected minimum = current + sold - received (if all stock was present 30 days ago).
            var expectedMinStock = p.CurrentQty + p.TotalSold - p.TotalReceived;
            if (expectedMinStock < 0)
            {
                var shrinkage = Math.Abs(expectedMinStock);
                alerts.Add(new AnomalyAlert
                {
                    AlertType = "InventoryShrinkage",
                    Severity = shrinkage > 10 ? "High" : "Medium",
                    Description = $"{p.Name}: ~{shrinkage} units unaccounted (sold={p.TotalSold}, received={p.TotalReceived}, current={p.CurrentQty})",
                    RelatedEntityId = p.Id,
                    RelatedEntityType = "Product",
                    AnomalyScore = Math.Min(1.0, shrinkage / 20.0)
                });
            }
        }

        logger.LogInformation("Detected {Count} inventory shrinkage alerts", alerts.Count);
        return alerts;
    }

    public async Task<IReadOnlyList<AnomalyAlert>> DetectPriceAnomaliesAsync(
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var alerts = new List<AnomalyAlert>();

        // Products priced significantly different from their category average.
        var categoryPrices = await context.Products
            .Where(p => p.IsActive && p.CategoryId != null)
            .GroupBy(p => p.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                AvgPrice = g.Average(p => p.SalePrice),
                StdDev = g.Count() > 1
                    ? (decimal)Math.Sqrt(g.Average(p => (double)((p.SalePrice - g.Average(x => x.SalePrice))
                        * (p.SalePrice - g.Average(x => x.SalePrice)))))
                    : 0m
            })
            .ToDictionaryAsync(g => g.CategoryId!.Value, ct)
            .ConfigureAwait(false);

        var products = await context.Products
            .Where(p => p.IsActive && p.CategoryId != null)
            .Select(p => new { p.Id, p.Name, p.SalePrice, p.CategoryId })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var p in products)
        {
            if (!categoryPrices.TryGetValue(p.CategoryId!.Value, out var cat)) continue;
            if (cat.StdDev == 0) continue;

            var zScore = (double)((p.SalePrice - cat.AvgPrice) / cat.StdDev);
            if (Math.Abs(zScore) > 2.5)
            {
                alerts.Add(new AnomalyAlert
                {
                    AlertType = "PriceAnomaly",
                    Severity = Math.Abs(zScore) > 3.5 ? "High" : "Medium",
                    Description = $"{p.Name}: {regionalSettings.FormatCurrency(p.SalePrice)} vs category avg {regionalSettings.FormatCurrency(cat.AvgPrice)} (z={zScore:F1})",
                    RelatedEntityId = p.Id,
                    RelatedEntityType = "Product",
                    AnomalyScore = Math.Min(1.0, Math.Abs(zScore) / 4.0)
                });
            }
        }

        logger.LogInformation("Detected {Count} price anomalies", alerts.Count);
        return alerts;
    }

    public async Task<IReadOnlyList<AnomalyAlert>> DetectDiscountAbuseAsync(
        int lookbackDays = 30, double thresholdPercent = 20, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = regionalSettings.Now.AddDays(-lookbackDays);
        var alerts = new List<AnomalyAlert>();

        var staffDiscounts = await context.Sales
            .Where(s => s.SaleDate >= cutoff && s.DiscountAmount > 0)
            .GroupBy(s => s.StaffId)
            .Select(g => new
            {
                StaffId = g.Key,
                DiscountedSales = g.Count(),
                TotalSales = context.Sales.Count(s => s.StaffId == g.Key && s.SaleDate >= cutoff),
                AvgDiscountPct = g.Average(s => s.TotalAmount > 0
                    ? (double)(s.DiscountAmount / s.TotalAmount * 100) : 0),
                MaxDiscount = g.Max(s => s.DiscountAmount)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var u in staffDiscounts)
        {
            if (u.AvgDiscountPct > thresholdPercent)
            {
                alerts.Add(new AnomalyAlert
                {
                    AlertType = "DiscountAbuse",
                    Severity = u.AvgDiscountPct > thresholdPercent * 2 ? "Critical" : "High",
                    Description = $"Staff {u.StaffId}: avg discount {u.AvgDiscountPct:F1}% across {u.DiscountedSales} sales (max {regionalSettings.FormatCurrency(u.MaxDiscount)})",
                    RelatedEntityId = u.StaffId,
                    RelatedEntityType = "Staff",
                    AnomalyScore = Math.Min(1.0, u.AvgDiscountPct / (thresholdPercent * 2))
                });
            }
        }

        logger.LogInformation("Detected {Count} discount abuse alerts", alerts.Count);
        return alerts;
    }

    public async Task<IReadOnlyList<AnomalyAlert>> DetectVoidPatternsAsync(
        int lookbackDays = 30, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var cutoff = regionalSettings.Now.AddDays(-lookbackDays);
        var alerts = new List<AnomalyAlert>();

        // Use SaleReturns as a proxy
        var staffReturns = await context.SaleReturns
            .Where(r => r.ReturnDate >= cutoff)
            .GroupBy(r => r.Sale!.StaffId)
            .Select(g => new
            {
                StaffId = g.Key,
                ReturnCount = g.Count(),
                ReturnTotal = g.Sum(r => r.RefundAmount),
                TotalSales = context.Sales.Count(s => s.StaffId == g.Key && s.SaleDate >= cutoff)
            })
            .Where(u => u.TotalSales >= 10)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Overall return rate baseline.
        var overallReturnRate = staffReturns.Sum(u => u.ReturnCount) /
            (double)Math.Max(1, staffReturns.Sum(u => u.TotalSales));

        foreach (var u in staffReturns)
        {
            var staffReturnRate = (double)u.ReturnCount / u.TotalSales;
            if (staffReturnRate > overallReturnRate * 2 && u.ReturnCount >= 3)
            {
                alerts.Add(new AnomalyAlert
                {
                    AlertType = "VoidPattern",
                    Severity = staffReturnRate > 0.25 ? "Critical" : "High",
                    Description = $"Staff {u.StaffId}: {staffReturnRate:P0} return rate ({u.ReturnCount} returns, {regionalSettings.FormatCurrency(u.ReturnTotal)}), baseline {overallReturnRate:P1}",
                    RelatedEntityId = u.StaffId,
                    RelatedEntityType = "Staff",
                    AnomalyScore = Math.Min(1.0, staffReturnRate / 0.3)
                });
            }
        }

        logger.LogInformation("Detected {Count} void pattern alerts", alerts.Count);
        return alerts;
    }

    public async Task<IReadOnlyList<AnomalyAlert>> RunFullScanAsync(
        int lookbackDays = 30, CancellationToken ct = default)
    {
        logger.LogInformation("Running full anomaly detection scan (lookback={Days} days)", lookbackDays);

        var results = await Task.WhenAll(
            DetectUnusualTransactionsAsync(lookbackDays, ct),
            DetectTheftPatternsAsync(lookbackDays, ct),
            DetectInventoryShrinkageAsync(ct),
            DetectPriceAnomaliesAsync(ct),
            DetectDiscountAbuseAsync(lookbackDays, ct: ct),
            DetectVoidPatternsAsync(lookbackDays, ct))
            .ConfigureAwait(false);

        var all = results.SelectMany(r => r).ToList();

        logger.LogInformation("Full anomaly scan complete: {Count} alerts total", all.Count);

        // Persist alerts so MarkReviewedAsync can find them.
        if (all.Count > 0)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            context.AnomalyAlerts.AddRange(all);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        // Publish high-severity alerts to event bus.
        foreach (var alert in all.Where(a => a.Severity is "Critical" or "High"))
            await eventBus.PublishAsync(new AnomalyDetectedEvent(alert)).ConfigureAwait(false);

        return all.OrderByDescending(a => a.AnomalyScore).ToList();
    }

    public async Task MarkReviewedAsync(int alertId, string? notes = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var alert = await context.AnomalyAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId, ct)
            .ConfigureAwait(false);

        if (alert is null)
        {
            logger.LogWarning("Alert {Id} not found for review", alertId);
            return;
        }

        alert.IsReviewed = true;
        alert.ReviewNotes = notes;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Alert {Id} marked as reviewed. Notes: {Notes}", alertId, notes);
    }
}
