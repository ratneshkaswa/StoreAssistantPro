using StoreAssistantPro.Models.AI;

namespace StoreAssistantPro.Modules.SmartFeatures.Services;

/// <summary>
/// Customer behavioral analysis and segmentation.
/// Features #527–530: purchase patterns (market basket), churn prediction,
/// segment auto-detection, next-purchase prediction.
/// </summary>
public interface ICustomerInsightsService
{
    /// <summary>Analyze what products customers buy together. (#527)</summary>
    Task<IReadOnlyList<ProductAssociation>> GetProductAssociationsAsync(
        int minSupport = 5, CancellationToken ct = default);

    /// <summary>Identify customers likely to stop buying. (#528)</summary>
    Task<IReadOnlyList<CustomerSegment>> GetChurnRiskCustomersAsync(
        double churnThreshold = 0.7, CancellationToken ct = default);

    /// <summary>Automatically group customers by behavior. (#529)</summary>
    Task<IReadOnlyList<CustomerSegment>> SegmentCustomersAsync(
        CancellationToken ct = default);

    /// <summary>Predict when a customer will buy next and what they'll buy. (#530)</summary>
    Task<(DateTime predictedDate, IReadOnlyList<int> likelyProductIds)> PredictNextPurchaseAsync(
        int customerId, CancellationToken ct = default);
}
