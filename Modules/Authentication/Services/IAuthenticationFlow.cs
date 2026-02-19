using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Authentication.Services;

/// <summary>
/// Encapsulates the Authentication module's window orchestration.
/// Consumers call these methods without knowing about specific
/// View or ViewModel types inside the module.
/// </summary>
public interface IAuthenticationFlow
{
    bool RunFirstTimeSetup();
    bool TryLogin(out UserType userType);
}
