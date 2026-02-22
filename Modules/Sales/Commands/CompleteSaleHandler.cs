using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Sales.Events;
using StoreAssistantPro.Modules.Sales.Services;

namespace StoreAssistantPro.Modules.Sales.Commands;

public class CompleteSaleHandler(
    ISalesService salesService,
    IEventBus eventBus,
    IRegionalSettingsService regional) : BaseCommandHandler<CompleteSaleCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(CompleteSaleCommand command)
    {
        var sale = new Sale
        {
            SaleDate = regional.Now,
            TotalAmount = command.TotalAmount,
            PaymentMethod = command.PaymentMethod,
            Items = command.Items.Select(i => new SaleItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        var saleId = await salesService.CreateSaleAsync(sale);
        await eventBus.PublishAsync(new SaleCompletedEvent(saleId, sale.TotalAmount));
        return CommandResult.Success();
    }
}
