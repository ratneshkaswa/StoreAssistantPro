using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Modules.Tax.Commands;

public class SetDefaultTaxProfileHandler(ITaxService taxService)
    : BaseCommandHandler<SetDefaultTaxProfileCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(SetDefaultTaxProfileCommand command)
    {
        await taxService.SetDefaultAsync(command.ProfileId);
        return CommandResult.Success();
    }
}
