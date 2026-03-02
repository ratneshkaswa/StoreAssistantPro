using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Users.Services;

public interface IUserService
{
    Task<IEnumerable<UserCredential>> GetAllUsersAsync(CancellationToken ct = default);
    Task ChangePinAsync(UserType userType, string newPin, CancellationToken ct = default);
    Task ClearLockoutAsync(UserType userType, CancellationToken ct = default);
}
