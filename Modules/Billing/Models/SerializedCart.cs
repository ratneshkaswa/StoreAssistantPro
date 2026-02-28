using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Models;

/// <summary>
/// JSON-serializable snapshot of the cart state persisted in
/// <see cref="BillingSession.SerializedBillData"/>.
/// <para>
/// Captures everything needed to restore a billing session:
/// line items (product + quantity + price at time of sale),
/// the bill-level discount, and the session correlation ID.
/// </para>
/// </summary>
public sealed class SerializedCart
{
    /// <summary>Correlation GUID matching <see cref="BillingSession.SessionId"/>.</summary>
    public Guid SessionId { get; init; }

    /// <summary>Line items in the cart at the time of save.</summary>
    public List<SerializedCartItem> Items { get; init; } = [];

    /// <summary>Bill-level discount applied at the time of save.</summary>
    public SerializedDiscount? Discount { get; init; }
}

/// <summary>
/// A single line item within the serialized cart.
/// Stores the product ID, quantity, and the unit price that was active
/// when the item was added (for audit trail and price-change detection).
/// </summary>
public sealed class SerializedCartItem
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }

    /// <summary>Unit sale price at the time of save.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Tax rate percentage at the time of save.</summary>
    public decimal TaxRate { get; init; }

    /// <summary>Whether the product's price included tax at save time.</summary>
    public bool IsTaxInclusive { get; init; }
}

/// <summary>
/// Serialized form of <see cref="BillDiscount"/>.
/// </summary>
public sealed class SerializedDiscount
{
    public DiscountType Type { get; init; }
    public decimal Value { get; init; }
    public string? Reason { get; init; }
}
