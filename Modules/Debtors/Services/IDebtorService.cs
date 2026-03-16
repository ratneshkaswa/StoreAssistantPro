using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Debtors.Services;

public interface IDebtorService
{
    Task<IReadOnlyList<Debtor>> GetAllAsync(CancellationToken ct = default);
    Task<Debtor?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(DebtorDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, DebtorDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task RecordPaymentAsync(int id, decimal amount, CancellationToken ct = default);
    Task<DebtorStats> GetStatsAsync(CancellationToken ct = default);
}

public record DebtorDto(
    string Name,
    string? Phone,
    decimal TotalAmount,
    decimal PaidAmount,
    DateTime Date,
    string? Note);

public record DebtorStats(
    int TotalDebtors,
    int PendingDebtors,
    decimal TotalOutstanding,
    decimal TotalCollected);
