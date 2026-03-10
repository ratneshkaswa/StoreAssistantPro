using StoreAssistantPro.Modules.Authentication.Commands;

namespace StoreAssistantPro.Modules.Authentication.Services;

public interface ISetupService
{
    Task InitializeAppAsync(CompleteFirstSetupCommand command, CancellationToken ct = default);
}
