using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Reports.Services;

public interface IReportsService
{
    Task<ExpenseReport> GetExpenseReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IroningReport> GetIroningReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<OrderReport> GetOrderReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<InwardReport> GetInwardReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<DebtorReport> GetDebtorReportAsync(CancellationToken ct = default);
}

public record ExpenseReport(
    int Count,
    decimal Total,
    IReadOnlyList<CategoryBreakdown> ByCategory,
    IReadOnlyList<MonthlyTotal> MonthlyTrend,
    IReadOnlyList<Expense> RecentEntries);

public record CategoryBreakdown(string Category, decimal Amount);

public record MonthlyTotal(string Month, decimal Amount);

public record IroningReport(
    int Count,
    decimal Total,
    decimal PaidTotal,
    decimal UnpaidTotal,
    IReadOnlyList<IroningEntry> RecentEntries);

public record OrderReport(
    int Count,
    decimal Total,
    int Delivered,
    int Pending,
    IReadOnlyList<Order> RecentEntries);

public record InwardReport(
    int Count,
    decimal Total,
    IReadOnlyList<InwardEntry> RecentEntries);

public record DebtorReport(
    int Count,
    decimal TotalOutstanding,
    IReadOnlyList<TopDebtor> TopDebtors);

public record TopDebtor(string Name, decimal Balance);
