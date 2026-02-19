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
        var result = await loginService.ValidatePinAsync(command.UserType, command.Pin);
        if (!result.Succeeded)
            return CommandResult.Failure(result.ErrorMessage ?? "Login failed.");

        await eventBus.PublishAsync(new UserLoggedInEvent(command.UserType));
        return CommandResult.Success();
    }
}
