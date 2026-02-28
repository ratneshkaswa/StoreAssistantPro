using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Sales.Events;

/// <summary>
/// Published when a sale is queued for offline sync instead of being
/// persisted directly to the server database. Subscribers (e.g. the
/// status bar, dashboard) can show a "queued" indicator.
/// </summary>
/// <param name="IdempotencyKey">
/// The idempotency key of the queued bill, matching
/// <see cref="Models.OfflineBill.IdempotencyKey"/>.
/// </param>
/// <param name="TotalAmount">
/// The bill total, for display in status bar or notification.
/// </param>
public sealed record SaleQueuedOfflineEvent(
    Guid IdempotencyKey,
    decimal TotalAmount) : IEvent;
