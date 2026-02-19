using StoreAssistantPro.Models;

namespace StoreAssistantPro.Session;

public interface ISessionService
{
    UserType CurrentUserType { get; }
    string FirmName { get; }
    bool IsLoggedIn { get; }
    Task LoginAsync(UserType userType);
    void Logout();
}
