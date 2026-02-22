namespace StoreAssistantPro.Models;

/// <summary>
/// Complete bill calculation result after applying discount and tax.
/// <para>
/// Calculation flow:
/// <code>
/// Subtotal        = Σ (SalePrice × Qty) for all line items
/// DiscountAmount  = applied from BillDiscount (flat or %)
/// TaxableAmount   = Subtotal − DiscountAmount
/// TaxAmount       = TaxableAmount × effectiveRate / 100
/// FinalAmount     = TaxableAmount + TaxAmount
/// </code>
/// </para>
/// </summary>
public sealed record BillSummary(
    decimal Subtotal,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal TaxAmount,
    decimal FinalAmount);
