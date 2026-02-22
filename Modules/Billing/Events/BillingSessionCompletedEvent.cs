using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Billing.Events;

/// <summary>
/// Published when a billing session finishes successfully — the sale
/// was saved and a receipt was generated.
/// <para>
/// <see cref="SmartBillingModeService"/> reacts by switching
/// <see cref="Models.OperationalMode"/> back to <c>Management</c>.
/// </para>
/// </summary>
public sealed record BillingSessionCompletedEvent : IEvent;
