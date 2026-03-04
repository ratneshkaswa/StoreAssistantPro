using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.FinancialYears.Services;

public interface IFinancialYearService
{
    Task<IReadOnlyList<FinancialYear>> GetAllAsync(CancellationToken ct = default);
    Task<FinancialYear?> GetCurrentAsync(CancellationToken ct = default);
    Task CreateAsync(DateTime startDate, CancellationToken ct = default);
    Task SetCurrentAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Ensures a financial year exists for the current date.
    /// If April 1 has passed and no FY exists, creates one and resets billing counters.
    /// Called at startup and on date rollover.
    /// </summary>
    Task EnsureCurrentYearAsync(CancellationToken ct = default);
}
