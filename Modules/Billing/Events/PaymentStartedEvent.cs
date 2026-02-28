using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Billing.Events;

/// <summary>
/// Published when payment processing begins for the active billing
/// session. Triggers an immediate flush of any pending auto-save so
/// the cart state is persisted before the payment outcome is resolved.
/// </summary>
/// <param name="SessionId">Correlation GUID of the active session.</param>
/// <param name="SerializedBillData">JSON snapshot at the moment payment starts.</param>
public sealed record PaymentStartedEvent(
    Guid SessionId,
    string SerializedBillData) : IEvent;
