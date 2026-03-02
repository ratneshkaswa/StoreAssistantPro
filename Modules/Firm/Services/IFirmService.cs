using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Firm.Services;

public interface IFirmService
{
    Task<AppConfig?> GetFirmAsync(CancellationToken ct = default);
    Task UpdateFirmAsync(FirmUpdateDto dto, CancellationToken ct = default);
}

public record FirmUpdateDto(
    string FirmName,
    string Address,
    string State,
    string Pincode,
    string Phone,
    string Email,
    string? GSTNumber,
    string? PANNumber,
    int FinancialYearStartMonth,
    int FinancialYearEndMonth,
    string CurrencySymbol,
    string DateFormat,
    string NumberFormat);
