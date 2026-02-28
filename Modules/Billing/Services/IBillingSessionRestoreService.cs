using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Deserializes a persisted <see cref="BillingSession"/>, validates every
/// line item against the current product catalog, recalculates totals
/// using <see cref="Core.Services.IPricingCalculationService"/>, and
/// returns a <see cref="RestoredCart"/> ready for display.
/// <para>
/// <b>Validation rules applied per item:</b>
/// </para>
/// <list type="bullet">
///   <item><b>Product deleted</b> → item skipped with
///         <see cref="SkipReason.ProductDeleted"/>.</item>
///   <item><b>Out of stock</b> (product exists, stock = 0) → item skipped
///         with <see cref="SkipReason.OutOfStock"/>.</item>
///   <item><b>Insufficient stock</b> (stock &lt; saved quantity) → quantity
///         clamped to available stock, item included.</item>
///   <item><b>Price changed</b> → item repriced at current catalog price,
///         <see cref="RestoredCartItem.PriceChanged"/> set to
///         <c>true</c>.</item>
///   <item><b>Tax profile changed</b> → current tax rate used.</item>
/// </list>
/// <para>
/// Registered as a <b>singleton</b>. All database access is async via
/// <c>IDbContextFactory</c>.
/// </para>
/// </summary>
public interface IBillingSessionRestoreService
{
    /// <summary>
    /// Restores a billing session from its serialized JSON data.
    /// </summary>
    /// <param name="session">The persisted session entity.</param>
    /// <returns>
    /// A fully validated and recalculated <see cref="RestoredCart"/>,
    /// or <c>null</c> if the JSON is corrupt or contains no items.
    /// </returns>
    Task<RestoredCart?> RestoreAsync(BillingSession session, CancellationToken ct = default);
}
