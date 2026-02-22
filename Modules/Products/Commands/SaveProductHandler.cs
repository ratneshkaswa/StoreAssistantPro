using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Products.Commands;

public class SaveProductHandler(IProductService productService)
    : BaseCommandHandler<SaveProductCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(SaveProductCommand command)
    {
        var product = new Product
        {
            Name = command.Name,
            SalePrice = command.SalePrice,
            Quantity = command.Quantity,
            TaxProfileId = command.TaxProfileId
        };

        await productService.AddAsync(product);
        return CommandResult.Success();
    }
}
