using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Users.Events;
using StoreAssistantPro.Modules.Users.Services;

namespace StoreAssistantPro.Modules.Users.Commands;

public class ChangePinHandler(
    IUserService userService,
    ILoginService loginService,
    IDbContextFactory<AppDbContext> contextFactory,
    IAppStateService appState,
    IEventBus eventBus) : BaseCommandHandler<ChangePinCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(ChangePinCommand command, CancellationToken ct)
    {
        // Master PIN validation — required for Admin, accepted for any role
        if (!string.IsNullOrWhiteSpace(command.MasterPin))
        {
            var isMasterValid = await loginService.ValidateMasterPinAsync(command.MasterPin, ct);
            if (!isMasterValid)
                return CommandResult.Failure("Invalid Master PIN.");
        }
        else if (command.UserType == UserType.Admin)
        {
            return CommandResult.Failure("Master PIN is required to change Admin PIN.");
        }

        await userService.ChangePinAsync(command.UserType, command.NewPin, ct);

        // Clear default PIN flag when admin changes their PIN
        if (command.UserType == UserType.Admin)
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);
            var config = await context.AppConfigs.SingleOrDefaultAsync(ct);
            if (config is { IsDefaultAdminPin: true })
            {
                config.IsDefaultAdminPin = false;
                await context.SaveChangesAsync(ct);
                appState.SetDefaultPinFlag(false);
            }
        }

        await eventBus.PublishAsync(new PinChangedEvent(command.UserType));
        return CommandResult.Success();
    }
}
