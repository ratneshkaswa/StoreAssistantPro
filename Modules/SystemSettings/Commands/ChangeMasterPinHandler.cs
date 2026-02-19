using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.SystemSettings.Services;

namespace StoreAssistantPro.Modules.SystemSettings.Commands;

public class ChangeMasterPinHandler(ISystemSettingsService settingsService)
    : BaseCommandHandler<ChangeMasterPinCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(ChangeMasterPinCommand command)
    {
        await settingsService.ChangeMasterPinAsync(command.CurrentPin, command.NewPin);
        return CommandResult.Success();
    }
}
