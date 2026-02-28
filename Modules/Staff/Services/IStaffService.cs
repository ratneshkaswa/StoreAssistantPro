using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Staff.Services;

public interface IStaffService
{
    Task<List<Models.Staff>> GetAllAsync();
    Task<Models.Staff?> GetByIdAsync(int id);
    Task<Models.Staff?> GetByCodeAsync(string code);
    Task<Models.Staff> CreateAsync(Models.Staff staff);
    Task UpdateAsync(Models.Staff staff);
    Task DeleteAsync(int id);
    Task<List<Models.Staff>> GetActiveStaffAsync();
}
