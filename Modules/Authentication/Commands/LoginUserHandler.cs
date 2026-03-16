using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Events;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public class LoginUserHandler(
    ILoginService loginService,
    IEventBus eventBus) : BaseCommandHandler<LoginUserCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(LoginUserCommand command, CancellationToken ct)
    {
        // User role logs in without PIN validation
        if (command.UserType == UserType.User)
        {
            await eventBus.PublishAsync(new UserLoggedInEvent(command.UserType));
            return CommandResult.Success();
        }

        var result = await loginService.ValidatePinAsync(command.UserType, command.Pin, ct);
        if (!result.Succeeded)
            return CommandResult.Failure(result.ErrorMessage ?? "Login failed.");

        await eventBus.PublishAsync(new UserLoggedInEvent(command.UserType));
        return CommandResult.Success();
    }
}
