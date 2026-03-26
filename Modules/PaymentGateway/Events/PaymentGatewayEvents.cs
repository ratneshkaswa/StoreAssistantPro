using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.PaymentGateway;

namespace StoreAssistantPro.Modules.PaymentGateway.Events;

/// <summary>Published when a gateway payment completes or fails.</summary>
public sealed class GatewayPaymentCompletedEvent(GatewayTransaction transaction) : IEvent
{
    public GatewayTransaction Transaction { get; } = transaction;
}

/// <summary>Published when payment reconciliation completes.</summary>
public sealed class ReconciliationCompletedEvent(int matchedCount, int unmatchedCount) : IEvent
{
    public int MatchedCount { get; } = matchedCount;
    public int UnmatchedCount { get; } = unmatchedCount;
}
