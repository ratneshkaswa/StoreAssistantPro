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
            command.FirmName, command.Address, command.State, command.Pincode,
            command.Phone, command.Email, command.GSTIN, command.PAN,
            command.CurrencyCode, command.CurrencySymbol,
            command.FinancialYearStartMonth, command.FinancialYearEndMonth,
            command.DateFormat,
            command.AdminPin, command.ManagerPin,
            command.UserPin, command.MasterPin, ct);

        return CommandResult.Success();
    }
}
