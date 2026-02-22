namespace StoreAssistantPro.Models;

/// <summary>
/// Immutable tax breakdown result for a single taxable amount.
/// <para>
/// For intra-state supply: <see cref="CGST"/> and <see cref="SGST"/> are populated,
/// <see cref="IGST"/> is zero.<br/>
/// For inter-state supply: <see cref="IGST"/> is populated,
/// <see cref="CGST"/> and <see cref="SGST"/> are zero.
/// </para>
/// </summary>
public sealed record TaxBreakdown(
    decimal BaseAmount,
    decimal CGST,
    decimal SGST,
    decimal IGST,
    decimal TotalTax,
    decimal TotalAmount);
