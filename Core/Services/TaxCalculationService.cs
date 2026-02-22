using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Stateless Indian GST calculation service.
/// All rounding uses <see cref="MidpointRounding.AwayFromZero"/>
/// (standard commercial rounding for Indian tax invoices).
/// </summary>
public class TaxCalculationService : ITaxCalculationService
{
    public TaxBreakdown Calculate(decimal amount, decimal taxRate, bool isIntraState)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        if (taxRate < 0 || taxRate > 100)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate must be between 0 and 100.");

        if (taxRate == 0 || amount == 0)
            return new TaxBreakdown(amount, 0m, 0m, 0m, 0m, amount);

        decimal cgst, sgst, igst;

        if (isIntraState)
        {
            var halfRate = taxRate / 2m;
            cgst = decimal.Round(amount * halfRate / 100m, 2, MidpointRounding.AwayFromZero);
            sgst = decimal.Round(amount * halfRate / 100m, 2, MidpointRounding.AwayFromZero);
            igst = 0m;
        }
        else
        {
            cgst = 0m;
            sgst = 0m;
            igst = decimal.Round(amount * taxRate / 100m, 2, MidpointRounding.AwayFromZero);
        }

        var totalTax = cgst + sgst + igst;
        var totalAmount = amount + totalTax;

        return new TaxBreakdown(amount, cgst, sgst, igst, totalTax, totalAmount);
    }

    public TaxBreakdown Calculate(decimal unitPrice, int quantity, decimal taxRate, bool isIntraState)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");

        var baseAmount = decimal.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero);
        return Calculate(baseAmount, taxRate, isIntraState);
    }
}
