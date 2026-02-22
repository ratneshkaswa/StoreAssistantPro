using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Models;

/// <summary>
/// Result of restoring a persisted billing session.
/// <para>
/// Contains the validated and recalculated cart ready for display,
/// plus metadata about items that could not be restored (deleted
/// products, insufficient stock, price changes).
/// </para>
/// </summary>
public sealed class RestoredCart
{
    /// <summary>Correlation GUID of the restored session.</summary>
    public Guid SessionId { get; init; }

    /// <summary>Successfully restored line items with recalculated totals.</summary>
    public IReadOnlyList<RestoredCartItem> Items { get; init; } = [];

    /// <summary>Bill-level discount (if one was saved).</summary>
    public BillDiscount Discount { get; init; } = BillDiscount.None;

    /// <summary>
    /// Items that were in the saved cart but could not be restored.
    /// Empty when all items restored successfully.
    /// </summary>
    public IReadOnlyList<SkippedCartItem> SkippedItems { get; init; } = [];

    /// <summary>Sum of all restored line subtotals.</summary>
    public decimal Subtotal { get; init; }

    /// <summary>Sum of all restored line tax amounts.</summary>
    public decimal TotalTax { get; init; }

    /// <summary>Grand total after tax (before bill-level discount).</summary>
    public decimal GrandTotal { get; init; }

    /// <summary><c>true</c> when one or more items were skipped during restore.</summary>
    public bool HasWarnings => SkippedItems.Count > 0;
}

/// <summary>
/// A line item that was successfully restored and repriced.
/// </summary>
public sealed class RestoredCartItem
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TaxRate { get; init; }
    public bool IsTaxInclusive { get; init; }

    /// <summary>Recalculated line total from <see cref="IPricingCalculationService"/>.</summary>
    public LineTotal LineTotal { get; init; } = null!;

    /// <summary>
    /// <c>true</c> when the current product price differs from the
    /// saved price. The restored item uses the <b>current</b> price.
    /// </summary>
    public bool PriceChanged { get; init; }
}

/// <summary>
/// Represents a saved cart item that could not be restored.
/// </summary>
public sealed class SkippedCartItem
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int SavedQuantity { get; init; }
    public SkipReason Reason { get; init; }
}

/// <summary>Why a saved item was excluded from the restored cart.</summary>
public enum SkipReason
{
    /// <summary>The product no longer exists in the database.</summary>
    ProductDeleted,

    /// <summary>The product exists but has zero stock.</summary>
    OutOfStock,
}
