using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Authentication.Services;

public interface ILoginService
{
    Task<LoginResult> ValidatePinAsync(UserType userType, string pin);
    Task<bool> ValidateMasterPinAsync(string pin);
}
