using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Expenses.Services;

public interface IExpenseService
{
    Task<IReadOnlyList<Expense>> GetAllAsync(CancellationToken ct = default);
    Task<Expense?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(ExpenseDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, ExpenseDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PettyCashDeposit>> GetDepositsAsync(CancellationToken ct = default);
    Task CreateDepositAsync(PettyCashDepositDto dto, CancellationToken ct = default);
    Task DeleteDepositAsync(int id, CancellationToken ct = default);
    Task<ExpenseStats> GetStatsAsync(CancellationToken ct = default);
}

public record ExpenseDto(DateTime Date, string Category, decimal Amount);

public record PettyCashDepositDto(DateTime Date, decimal Amount, string? Note);

public record ExpenseStats(
    decimal TotalExpenses,
    decimal TotalDeposits,
    decimal Balance,
    int ExpenseCount,
    decimal TodaySpent,
    decimal ThisMonthSpent,
    decimal LastMonthSpent);
