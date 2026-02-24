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
            CostPrice = command.CostPrice,
            Quantity = command.Quantity,
            TaxProfileId = command.TaxProfileId,
            HSNCode = string.IsNullOrWhiteSpace(command.HSNCode) ? null : command.HSNCode.Trim(),
            Barcode = string.IsNullOrWhiteSpace(command.Barcode) ? null : command.Barcode.Trim(),
            UOM = string.IsNullOrWhiteSpace(command.UOM) ? "pcs" : command.UOM.Trim(),
            MinStockLevel = command.MinStockLevel,
            IsActive = command.IsActive,
            IsTaxInclusive = command.IsTaxInclusive
        };

        await productService.AddAsync(product);
        return CommandResult.Success();
    }
}
