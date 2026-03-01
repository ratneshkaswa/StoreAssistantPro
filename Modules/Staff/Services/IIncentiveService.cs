using StoreAssistantPro.Modules.Reports.Models;

namespace StoreAssistantPro.Modules.Staff.Services;

public interface IIncentiveService
{
    Task<List<StaffIncentiveSummary>> GetStaffIncentivesAsync(DateTime from, DateTime to);
    Task<StaffIncentiveSummary?> GetStaffIncentiveAsync(int staffId, DateTime from, DateTime to);
    Task<List<StaffIncentiveSummary>> GetTodaysIncentivesAsync();
}
