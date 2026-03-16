using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Branch.Services;

public interface IBranchBillService
{
    Task<IReadOnlyList<BranchBill>> GetAllAsync(string? type = null, CancellationToken ct = default);
    Task<BranchBill?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(BranchBillDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, BranchBillDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task MarkClearedAsync(int id, CancellationToken ct = default);
    Task<int> ClearAllAsync(int retentionDays = 30, CancellationToken ct = default);
    Task<BranchStats> GetStatsAsync(CancellationToken ct = default);
}

public record BranchBillDto(
    DateTime Date,
    string BillNo,
    decimal Amount,
    string Type);

public record BranchStats(
    int TotalBills,
    int PendingBills,
    decimal TotalAmount,
    decimal ClearedAmount);
