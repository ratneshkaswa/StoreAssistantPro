using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Billing.Events;

/// <summary>
/// Published when a billing session is abandoned — the cart was
/// discarded without completing a sale.
/// <para>
/// <see cref="SmartBillingModeService"/> reacts by switching
/// <see cref="Models.OperationalMode"/> back to <c>Management</c>.
/// </para>
/// </summary>
public sealed record BillingSessionCancelledEvent : IEvent;
