using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Users.Services;

public interface IUserService
{
    Task<IEnumerable<UserCredential>> GetAllUsersAsync();
    Task ChangePinAsync(UserType userType, string newPin);
    Task ClearLockoutAsync(UserType userType);
}
