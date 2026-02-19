using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Firm.Services;

public interface IFirmService
{
    Task<AppConfig?> GetFirmAsync();
    Task UpdateFirmAsync(string firmName, string address, string phone);
}
