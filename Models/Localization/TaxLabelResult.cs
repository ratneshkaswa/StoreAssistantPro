namespace StoreAssistantPro.Models.Localization;

/// <summary>
/// Determines whether a transaction is intra-state (CGST+SGST) or
/// inter-state (IGST) based on seller and buyer state codes.
/// </summary>
public sealed record TaxLabelResult(
    bool IsInterState,
    string CentralTaxLabel,
    string StateTaxLabel,
    string IntegratedTaxLabel,
    decimal CentralTaxRate,
    decimal StateTaxRate,
    decimal IntegratedTaxRate);

/// <summary>
/// Regional receipt template configuration.
/// </summary>
public sealed record RegionalReceiptConfig(
    string Language,
    string HeaderTemplate,
    string ItemLineTemplate,
    string TotalLabel,
    string TaxLabel,
    string FooterTemplate,
    string ThankYouMessage);
