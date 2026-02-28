using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Session;

public interface ISessionService
{
    UserType CurrentUserType { get; }
    string FirmName { get; }
    bool IsLoggedIn { get; }
    Task LoginAsync(UserType userType);
    Task RefreshFirmNameAsync();
    void Logout();
}
