namespace StoreAssistantPro.Modules.Firm.Services;

public interface IFirmService
{
    Task<FirmManagementSnapshot?> GetFirmAsync(CancellationToken ct = default);
    Task UpdateFirmAsync(FirmUpdateDto dto, CancellationToken ct = default);
}

public sealed record FirmManagementSnapshot(
    string FirmName,
    string Address,
    string State,
    string Pincode,
    string Phone,
    string Email,
    string? GSTNumber,
    string? PANNumber,
    string GstRegistrationType,
    decimal CompositionSchemeRate,
    string? StateCode,
    int FinancialYearStartMonth,
    int FinancialYearEndMonth,
    string CurrencySymbol,
    string DateFormat,
    string NumberFormat,
    string DefaultTaxMode,
    string RoundingMethod,
    bool NegativeStockAllowed,
    string NumberToWordsLanguage);

public record FirmUpdateDto(
    string FirmName,
    string Address,
    string State,
    string Pincode,
    string Phone,
    string Email,
    string? GSTNumber,
    string? PANNumber,
    string GstRegistrationType,
    decimal CompositionSchemeRate,
    string? StateCode,
    int FinancialYearStartMonth,
    int FinancialYearEndMonth,
    string CurrencySymbol,
    string DateFormat,
    string NumberFormat,
    string DefaultTaxMode,
    string RoundingMethod,
    bool NegativeStockAllowed,
    string NumberToWordsLanguage);
