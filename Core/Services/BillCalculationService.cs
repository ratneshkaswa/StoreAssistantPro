using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Stateless bill-level calculation service.
/// All rounding uses <see cref="MidpointRounding.AwayFromZero"/>
/// (standard commercial rounding for Indian tax invoices).
/// </summary>
public class BillCalculationService : IBillCalculationService
{
    public BillSummary Calculate(decimal lineSubtotal, decimal taxRate, BillDiscount? discount = null)
    {
        if (lineSubtotal < 0)
            throw new ArgumentOutOfRangeException(nameof(lineSubtotal), "Subtotal cannot be negative.");
        if (taxRate < 0 || taxRate > 100)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate must be between 0 and 100.");

        discount ??= BillDiscount.None;

        var discountType = discount.Type;
        var discountValue = discount.Value;

        // ── Compute discount amount ────────────────────────────────
        var discountAmount = discountType switch
        {
            DiscountType.Amount => decimal.Round(
                Math.Min(discountValue, lineSubtotal), 2, MidpointRounding.AwayFromZero),

            DiscountType.Percentage => discountValue > 100
                ? throw new ArgumentOutOfRangeException(
                    nameof(discount), "Discount percentage cannot exceed 100.")
                : decimal.Round(
                    lineSubtotal * discountValue / 100m, 2, MidpointRounding.AwayFromZero),

            _ => 0m // None
        };

        // ── Taxable amount (post-discount, pre-tax) ────────────────
        var taxableAmount = lineSubtotal - discountAmount;

        // ── Tax on discounted amount ───────────────────────────────
        var taxAmount = taxRate == 0 || taxableAmount == 0
            ? 0m
            : decimal.Round(taxableAmount * taxRate / 100m, 2, MidpointRounding.AwayFromZero);

        var finalAmount = taxableAmount + taxAmount;

        return new BillSummary(
            lineSubtotal,
            discountType,
            discountValue,
            discountAmount,
            taxableAmount,
            taxAmount,
            finalAmount);
    }
}
