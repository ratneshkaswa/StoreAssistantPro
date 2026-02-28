using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Billing.Events;

/// <summary>
/// Published when the in-memory cart state changes during an active
/// billing session — product added, quantity changed, item removed,
/// or discount applied.
/// <para>
/// <see cref="BillingAutoSaveService"/> reacts by scheduling a
/// debounced persistence save so the cart can be recovered after a
/// crash or restart.
/// </para>
/// </summary>
/// <param name="SessionId">Correlation GUID of the active session.</param>
/// <param name="SerializedBillData">JSON snapshot of the current cart state.</param>
public sealed record CartChangedEvent(
    Guid SessionId,
    string SerializedBillData) : IEvent;
