using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Modules.Authentication.Events;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public class LogoutHandler(
    ISessionService sessionService,
    IAppStateService appState,
    IEventBus eventBus) : BaseCommandHandler<LogoutCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(LogoutCommand command)
    {
        // 1. Clear billing session before state reset
        appState.SetBillingSession(null);

        // 2. Full session teardown
        sessionService.Logout();

        // 3. Clear all remaining AppState (notifications, etc.)
        appState.ClearNotifications();

        // 4. Notify all subscribers
        await eventBus.PublishAsync(new UserLoggedOutEvent(command.UserType));

        return CommandResult.Success();
    }
}
