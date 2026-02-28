using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Computes line-item pricing for retail billing.
/// <para>
/// Handles both tax-exclusive and tax-inclusive pricing modes:
/// <list type="bullet">
///   <item><b>Tax-exclusive</b> (<c>IsTaxInclusive = false</c>):
///     Subtotal = SalePrice × Qty, tax added on top.</item>
///   <item><b>Tax-inclusive</b> (<c>IsTaxInclusive = true</c>):
///     SalePrice × Qty is the shelf price; base and tax are
///     back-calculated so FinalAmount equals the shelf total.</item>
/// </list>
/// </para>
/// <para>
/// This service has no discount logic — discounts are applied
/// at the billing level, not per-product.
/// </para>
/// </summary>
public interface IPricingCalculationService
{
    /// <summary>
    /// Calculates subtotal, tax, and final amount for a line item.
    /// </summary>
    /// <param name="salePrice">Unit selling price of the product.</param>
    /// <param name="quantity">Number of units.</param>
    /// <param name="taxRate">Composite GST rate percentage (e.g. 18 for 18%).</param>
    /// <param name="isTaxInclusive">
    /// <c>true</c> if <paramref name="salePrice"/> already includes tax.
    /// </param>
    LineTotal CalculateLineTotal(decimal salePrice, int quantity, decimal taxRate, bool isTaxInclusive);
}
