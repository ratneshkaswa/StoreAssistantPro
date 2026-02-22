using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Computes GST tax breakdowns for Indian retail transactions.
/// <para>
/// <b>Intra-state</b> (seller and buyer in same state):
/// CGST = rate / 2, SGST = rate / 2.<br/>
/// <b>Inter-state</b> (seller and buyer in different states):
/// IGST = rate.
/// </para>
/// <para>
/// The service is stateless and does not access the database.
/// Tax rates come from <see cref="TaxMaster"/> / <see cref="TaxProfile"/>
/// entities resolved by the caller.
/// </para>
/// </summary>
public interface ITaxCalculationService
{
    /// <summary>
    /// Calculates the tax breakdown for a single taxable amount.
    /// </summary>
    /// <param name="amount">Base (pre-tax) amount.</param>
    /// <param name="taxRate">Composite tax rate percentage (e.g. 18 for 18%).</param>
    /// <param name="isIntraState">
    /// <c>true</c> → split into CGST + SGST;
    /// <c>false</c> → full rate as IGST.
    /// </param>
    TaxBreakdown Calculate(decimal amount, decimal taxRate, bool isIntraState);

    /// <summary>
    /// Calculates the tax breakdown for a line item (quantity × unit price).
    /// </summary>
    TaxBreakdown Calculate(decimal unitPrice, int quantity, decimal taxRate, bool isIntraState);
}
