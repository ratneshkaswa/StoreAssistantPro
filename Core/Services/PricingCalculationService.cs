using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Stateless retail pricing calculator.
/// All rounding uses <see cref="MidpointRounding.AwayFromZero"/>
/// (standard commercial rounding for Indian tax invoices).
/// </summary>
public class PricingCalculationService : IPricingCalculationService
{
    public LineTotal CalculateLineTotal(decimal salePrice, int quantity, decimal taxRate, bool isTaxInclusive)
    {
        if (salePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(salePrice), "Sale price cannot be negative.");
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        if (taxRate < 0 || taxRate > 100)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate must be between 0 and 100.");

        var lineAmount = decimal.Round(salePrice * quantity, 2, MidpointRounding.AwayFromZero);

        if (taxRate == 0 || lineAmount == 0)
            return new LineTotal(lineAmount, 0m, lineAmount);

        if (isTaxInclusive)
        {
            // SalePrice includes tax — back-calculate base
            // Base = LineAmount / (1 + rate/100)
            var divisor = 1m + taxRate / 100m;
            var subtotal = decimal.Round(lineAmount / divisor, 2, MidpointRounding.AwayFromZero);
            var taxAmount = lineAmount - subtotal;
            return new LineTotal(subtotal, taxAmount, lineAmount);
        }
        else
        {
            // SalePrice is base — tax added on top
            var taxAmount = decimal.Round(lineAmount * taxRate / 100m, 2, MidpointRounding.AwayFromZero);
            var finalAmount = lineAmount + taxAmount;
            return new LineTotal(lineAmount, taxAmount, finalAmount);
        }
    }
}
