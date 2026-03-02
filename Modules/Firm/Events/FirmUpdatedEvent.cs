using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Firm.Events;

public sealed record FirmUpdatedEvent(
    string FirmName,
    string CurrencySymbol,
    string DateFormat) : IEvent;
