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
        if (!string.IsNullOrWhiteSpace(command.Barcode)
            && !await productService.IsBarcodeUniqueAsync(command.Barcode))
        {
            return CommandResult.Failure($"Barcode '{command.Barcode.Trim()}' is already assigned to another product.");
        }

        var product = new Product
        {
            Name = command.Name,
            SalePrice = command.SalePrice,
            CostPrice = command.CostPrice,
            Quantity = command.Quantity,
            TaxProfileId = command.TaxProfileId,
            BrandId = command.BrandId,
            HSNCode = string.IsNullOrWhiteSpace(command.HSNCode) ? null : command.HSNCode.Trim(),
            Barcode = string.IsNullOrWhiteSpace(command.Barcode) ? null : command.Barcode.Trim(),
            UOM = string.IsNullOrWhiteSpace(command.UOM) ? "pcs" : command.UOM.Trim(),
            MinStockLevel = command.MinStockLevel,
            MaxStockLevel = command.MaxStockLevel,
            IsActive = command.IsActive,
            IsTaxInclusive = command.IsTaxInclusive,
            Color = string.IsNullOrWhiteSpace(command.Color) ? null : command.Color.Trim()
        };

        await productService.AddAsync(product);
        return CommandResult.Success();
    }
}
