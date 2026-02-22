namespace StoreAssistantPro.Models;

/// <summary>
/// Pricing result for a single line item in a retail transaction.
/// Contains the amounts needed for cart display and invoice totals.
/// For GST component breakdown (CGST/SGST/IGST), use
/// <see cref="TaxBreakdown"/> via <c>ITaxCalculationService</c> directly.
/// </summary>
public sealed record LineTotal(
    decimal Subtotal,
    decimal TaxAmount,
    decimal FinalAmount);
