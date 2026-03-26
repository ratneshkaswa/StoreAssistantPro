using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.NicheVertical.Events;

/// <summary>Published when an alteration order status changes.</summary>
public sealed class AlterationStatusChangedEvent(int orderId, string newStatus) : IEvent
{
    public int OrderId { get; } = orderId;
    public string NewStatus { get; } = newStatus;
}

/// <summary>Published when a rental item is returned.</summary>
public sealed class RentalReturnedEvent(int rentalRecordId) : IEvent
{
    public int RentalRecordId { get; } = rentalRecordId;
}

/// <summary>Published when a gift card is redeemed.</summary>
public sealed class GiftCardRedeemedEvent(string cardNumber, decimal amount) : IEvent
{
    public string CardNumber { get; } = cardNumber;
    public decimal Amount { get; } = amount;
}
