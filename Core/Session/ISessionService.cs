using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Session;

public interface ISessionService
{
    UserType CurrentUserType { get; }
    string FirmName { get; }
    bool IsLoggedIn { get; }
    Task LoginAsync(UserType userType, CancellationToken ct = default);
    Task RefreshFirmNameAsync(CancellationToken ct = default);
    void Logout();
}
