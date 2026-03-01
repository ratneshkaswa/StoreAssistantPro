using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Sales.Events;

public sealed record SaleCompletedEvent(
    int SaleId,
    decimal TotalAmount,
    int? CustomerId = null) : IEvent;
