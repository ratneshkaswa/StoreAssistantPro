using StoreAssistantPro.Models.AI;

namespace StoreAssistantPro.Modules.SmartFeatures.Services;

/// <summary>
/// Anomaly and fraud detection engine.
/// Features #535–540: unusual transaction alerts, theft detection,
/// inventory shrinkage, price anomaly, discount abuse, void pattern.
/// </summary>
public interface IAnomalyDetectionService
{
    /// <summary>Flag transactions with abnormal patterns. (#535)</summary>
    Task<IReadOnlyList<AnomalyAlert>> DetectUnusualTransactionsAsync(
        int lookbackDays = 30, CancellationToken ct = default);

    /// <summary>Detect patterns suggesting employee theft. (#536)</summary>
    Task<IReadOnlyList<AnomalyAlert>> DetectTheftPatternsAsync(
        int lookbackDays = 30, CancellationToken ct = default);

    /// <summary>Alert when stock decreases faster than sales explain. (#537)</summary>
    Task<IReadOnlyList<AnomalyAlert>> DetectInventoryShrinkageAsync(
        CancellationToken ct = default);

    /// <summary>Flag products priced significantly different from category average. (#538)</summary>
    Task<IReadOnlyList<AnomalyAlert>> DetectPriceAnomaliesAsync(
        CancellationToken ct = default);

    /// <summary>Detect excessive/suspicious discounting by specific users. (#539)</summary>
    Task<IReadOnlyList<AnomalyAlert>> DetectDiscountAbuseAsync(
        int lookbackDays = 30, double thresholdPercent = 20, CancellationToken ct = default);

    /// <summary>Alert on unusual void/cancel rates per cashier. (#540)</summary>
    Task<IReadOnlyList<AnomalyAlert>> DetectVoidPatternsAsync(
        int lookbackDays = 30, CancellationToken ct = default);

    /// <summary>Run all anomaly detection checks and return combined results.</summary>
    Task<IReadOnlyList<AnomalyAlert>> RunFullScanAsync(
        int lookbackDays = 30, CancellationToken ct = default);

    /// <summary>Mark an alert as reviewed.</summary>
    Task MarkReviewedAsync(int alertId, string? notes = null, CancellationToken ct = default);
}
