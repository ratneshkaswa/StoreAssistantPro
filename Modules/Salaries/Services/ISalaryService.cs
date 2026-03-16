using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Salaries.Services;

public interface ISalaryService
{
    Task<IReadOnlyList<Salary>> GetAllAsync(CancellationToken ct = default);
    Task<Salary?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(SalaryDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, SalaryDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task MarkPaidAsync(int id, CancellationToken ct = default);
    Task<SalaryStats> GetStatsAsync(CancellationToken ct = default);
}

public record SalaryDto(
    string EmployeeName,
    string Month,
    int Year,
    decimal Amount,
    decimal BaseSalary,
    decimal Advance,
    int PresentDays,
    int AbsentDays,
    decimal HoursWorked,
    decimal Incentive,
    string? Note);

public record SalaryStats(
    int Total,
    int Paid,
    int Unpaid,
    decimal TotalPaid,
    decimal TotalPending);
