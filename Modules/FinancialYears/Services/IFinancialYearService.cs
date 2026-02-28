using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.FinancialYears.Services;

public interface IFinancialYearService
{
    Task<List<FinancialYear>> GetAllAsync();
    Task<FinancialYear?> GetCurrentAsync();
    Task<FinancialYear> CreateAsync(FinancialYear fy);
    Task SetCurrentAsync(int id);
    Task<FinancialYear> EnsureCurrentFYAsync();
}
