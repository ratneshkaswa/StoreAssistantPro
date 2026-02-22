using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Modules.Tax.Commands;

public class ToggleTaxProfileHandler(ITaxService taxService)
    : BaseCommandHandler<ToggleTaxProfileCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(ToggleTaxProfileCommand command)
    {
        if (!command.IsActive)
        {
            var inUse = await taxService.IsProfileUsedByProductsAsync(command.ProfileId);
            if (inUse)
                return CommandResult.Failure(
                    "Cannot deactivate — this tax profile is assigned to one or more products.");
        }

        await taxService.SetActiveAsync(command.ProfileId, command.IsActive);
        return CommandResult.Success();
    }
}
