using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Billing.Events;

/// <summary>
/// Published when a new billing session begins — the operator is
/// building a cart or processing a payment.
/// <para>
/// <see cref="SmartBillingModeService"/> reacts by switching
/// <see cref="Models.OperationalMode"/> to <c>Billing</c>.
/// </para>
/// </summary>
public sealed record BillingSessionStartedEvent : IEvent;
