using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Modules.Authentication.Events;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public class LoginUserHandler(
    ILoginService loginService,
    IEventBus eventBus) : BaseCommandHandler<LoginUserCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(LoginUserCommand command)
    {
        var isValid = await loginService.ValidatePinAsync(command.UserType, command.Pin);
        if (!isValid)
            return CommandResult.Failure("Invalid PIN. Try again.");

        await eventBus.PublishAsync(new UserLoggedInEvent(command.UserType));
        return CommandResult.Success();
    }
}
