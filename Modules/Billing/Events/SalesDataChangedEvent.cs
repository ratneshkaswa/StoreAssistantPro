using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Billing.Events;

/// <summary>
/// Published after sales data changes so cached analytical screens can
/// invalidate their service snapshots and mark themselves stale.
/// </summary>
public sealed record SalesDataChangedEvent(string Reason, DateTime OccurredAt) : IEvent;
