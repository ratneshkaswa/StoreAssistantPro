namespace StoreAssistantPro.Modules.Quotations.Services;

/// <summary>Formatted quotation data for printing (#452).</summary>
public record QuotationPrintData(
    string QuoteNumber,
    DateTime QuoteDate,
    DateTime ValidUntil,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerAddress,
    string? CustomerGSTIN,
    string FirmName,
    string FirmAddress,
    string FirmPhone,
    string? FirmGSTIN,
    string? Notes,
    string? TermsAndConditions,
    int RevisionNumber,
    decimal TotalAmount,
    IReadOnlyList<QuotationPrintLine> Items);

/// <summary>Individual line item in a quotation print (#452).</summary>
public record QuotationPrintLine(
    string ProductName,
    string? HsnCode,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal TaxRate,
    decimal CessRate,
    decimal Subtotal,
    decimal TaxAmount,
    decimal CessAmount,
    decimal LineTotal);
