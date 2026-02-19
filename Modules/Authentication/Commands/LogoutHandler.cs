using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Modules.Authentication.Events;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public class LogoutHandler(
    ISessionService sessionService,
    IEventBus eventBus,
    ILogger<LogoutHandler> logger) : BaseCommandHandler<LogoutCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(LogoutCommand command)
    {
        logger.LogInformation("Logout initiated for {UserType}", command.UserType);

        // SessionService.Logout() clears session + calls appState.Reset()
        sessionService.Logout();

        await eventBus.PublishAsync(new UserLoggedOutEvent(command.UserType));

        logger.LogInformation("Logout completed for {UserType}", command.UserType);
        return CommandResult.Success();
    }
}
