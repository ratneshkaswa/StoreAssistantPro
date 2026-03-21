using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Expenses.Services;

public interface IExpenseService
{
    Task<IReadOnlyList<Expense>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<Expense>> GetPagedAsync(PagedQuery query, string? search = null, string? dateFilter = null, CancellationToken ct = default);
    Task<Expense?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(ExpenseDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, ExpenseDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PettyCashDeposit>> GetDepositsAsync(CancellationToken ct = default);
    Task CreateDepositAsync(PettyCashDepositDto dto, CancellationToken ct = default);
    Task DeleteDepositAsync(int id, CancellationToken ct = default);
    Task<ExpenseStats> GetStatsAsync(CancellationToken ct = default);
    Task<int> ImportBulkAsync(IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default);

    // ── Expense Categories (#227/#228) ──

    Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(CancellationToken ct = default);
    Task CreateCategoryAsync(string name, CancellationToken ct = default);
    Task UpdateCategoryAsync(int id, string name, CancellationToken ct = default);
    Task DeleteCategoryAsync(int id, CancellationToken ct = default);
    Task SeedDefaultCategoriesAsync(CancellationToken ct = default);

    // ── Monthly expense report (#232) ──

    Task<MonthlyExpenseReport> GetMonthlyExpenseReportAsync(int year, int month, CancellationToken ct = default);
}

public record ExpenseDto(
    DateTime Date,
    string Category,
    decimal Amount,
    string PaymentMethod = "Cash",
    string? Description = null,
    string? CreatedBy = null);

public record PettyCashDepositDto(DateTime Date, decimal Amount, string? Note);

public record ExpenseStats(
    decimal TotalExpenses,
    decimal TotalDeposits,
    decimal Balance,
    int ExpenseCount,
    decimal TodaySpent,
    decimal ThisMonthSpent,
    decimal LastMonthSpent);

public record MonthlyExpenseReport(
    int Year,
    int Month,
    decimal TotalAmount,
    int ExpenseCount,
    IReadOnlyList<CategoryExpenseBreakdown> ByCategory);

public record CategoryExpenseBreakdown(string Category, decimal Amount, int Count);
