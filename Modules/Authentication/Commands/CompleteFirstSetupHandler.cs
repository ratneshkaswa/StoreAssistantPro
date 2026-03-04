using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public class CompleteFirstSetupHandler(ISetupService setupService)
    : BaseCommandHandler<CompleteFirstSetupCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(CompleteFirstSetupCommand command, CancellationToken ct)
    {
        await setupService.InitializeAppAsync(
            command.FirmName, command.Address, command.Phone,
            command.Email, command.GSTIN, command.CurrencyCode,
            command.AdminPin, command.ManagerPin,
            command.UserPin, command.MasterPin, ct);

        return CommandResult.Success();
    }
}
