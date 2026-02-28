using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Users.Events;
using StoreAssistantPro.Modules.Users.Services;

namespace StoreAssistantPro.Modules.Users.Commands;

public class ChangePinHandler(
    IUserService userService,
    ILoginService loginService,
    IEventBus eventBus) : BaseCommandHandler<ChangePinCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(ChangePinCommand command)
    {
        if (command.UserType == UserType.Admin)
        {
            if (string.IsNullOrWhiteSpace(command.MasterPin))
                return CommandResult.Failure("Master Password is required to change Admin PIN.");

            var isMasterValid = await loginService.ValidateMasterPinAsync(command.MasterPin);
            if (!isMasterValid)
                return CommandResult.Failure("Invalid Master Password.");
        }

        await userService.ChangePinAsync(command.UserType, command.NewPin);
        await eventBus.PublishAsync(new PinChangedEvent(command.UserType));
        return CommandResult.Success();
    }
}
