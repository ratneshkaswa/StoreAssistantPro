using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Events;

/// <summary>
/// Published when the billing session state changes.
/// Subscribers use this to update UI elements (e.g., status bar session
/// indicator) without direct coupling to the session service.
/// </summary>
public sealed record BillingSessionStateChangedEvent(
    BillingSessionState PreviousState,
    BillingSessionState NewState) : IEvent;
