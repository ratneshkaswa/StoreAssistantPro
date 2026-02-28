using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Billing.Events;

/// <summary>
/// Published when <see cref="Services.ZeroClickProductAddService"/>
/// automatically adds a product to the active billing session.
/// <para>
/// Subscribers can use this to update UI state (e.g., clear the search
/// input, show a toast confirmation, update cart totals).
/// </para>
/// </summary>
/// <param name="ProductId">The product that was auto-added.</param>
/// <param name="ProductName">Snapshot of the product name for display.</param>
/// <param name="Quantity">Quantity added (always 1 for zero-click adds).</param>
/// <param name="UnitPrice">Sale price per unit at time of add.</param>
/// <param name="Source">How the product was identified (<c>"Barcode"</c> or <c>"ExactMatch"</c>).</param>
public sealed record ProductAddedToCartEvent(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    string Source) : IEvent;
