using StoreAssistantPro.Models;

namespace StoreAssistantPro.Services;

public interface ILoginService
{
    Task<bool> ValidatePinAsync(UserType userType, string pin);
    Task<bool> ValidateMasterPinAsync(string pin);
}
