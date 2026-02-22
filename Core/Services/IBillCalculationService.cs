using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Calculates bill-level totals for retail POS transactions.
/// <para>
/// Flow: Subtotal → Discount → Tax → Final.
/// Discount is applied <b>before</b> tax so tax is computed on the
/// discounted amount (standard Indian GST practice for trade discounts
/// shown on the invoice).
/// </para>
/// <para>
/// This service is stateless and contains no UI or persistence logic.
/// </para>
/// </summary>
public interface IBillCalculationService
{
    /// <summary>
    /// Computes the full bill summary.
    /// </summary>
    /// <param name="lineSubtotal">
    /// Pre-discount subtotal (sum of all line item amounts).
    /// For tax-inclusive products, this should be the sum of base amounts
    /// (after back-calculation via <see cref="IPricingCalculationService"/>).
    /// </param>
    /// <param name="taxRate">Composite GST rate percentage (e.g. 18 for 18%).</param>
    /// <param name="discount">
    /// Optional bill-level discount. Pass <see cref="BillDiscount.None"/>
    /// or <c>null</c> for no discount.
    /// </param>
    BillSummary Calculate(decimal lineSubtotal, decimal taxRate, BillDiscount? discount = null);
}
