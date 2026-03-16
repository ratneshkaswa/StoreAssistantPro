using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.SalesPurchase.Services;

public interface ISalesPurchaseService
{
    Task<IReadOnlyList<SalesPurchaseEntry>> GetAllAsync(string? type = null, CancellationToken ct = default);
    Task<SalesPurchaseEntry?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(SalesPurchaseEntryDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, SalesPurchaseEntryDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<SalesPurchaseStats> GetStatsAsync(CancellationToken ct = default);
}

public record SalesPurchaseEntryDto(
    DateTime Date,
    string Note,
    decimal Amount,
    string Type);

public record SalesPurchaseStats(
    decimal TotalSales,
    decimal TotalPurchases,
    decimal NetBalance,
    int EntryCount);
