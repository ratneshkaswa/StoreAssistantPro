using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Sales.Services;

namespace StoreAssistantPro.Modules.Sales.Commands;

public class ProcessReturnHandler(
    ISaleReturnService returnService,
    ISessionService session) : BaseCommandHandler<ProcessReturnCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(ProcessReturnCommand command)
    {
        var returnNumber = await returnService.GenerateReturnNumberAsync();

        var saleReturn = new SaleReturn
        {
            ReturnNumber = returnNumber,
            SaleId = command.SaleId,
            SaleItemId = command.SaleItemId,
            Quantity = command.Quantity,
            RefundAmount = command.RefundAmount,
            Reason = command.Reason,
            StockRestored = command.RestoreStock,
            ProcessedByRole = session.CurrentUserType.ToString()
        };

        await returnService.ProcessReturnAsync(saleReturn);
        return CommandResult.Success();
    }
}
