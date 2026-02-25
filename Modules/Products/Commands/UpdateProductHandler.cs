using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Products.Commands;

public class UpdateProductHandler(IProductService productService)
    : BaseCommandHandler<UpdateProductCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(UpdateProductCommand command)
    {
        var product = await productService.GetByIdAsync(command.ProductId);
        if (product is null)
            return CommandResult.Failure("Product not found.");

        if (!string.IsNullOrWhiteSpace(command.Barcode)
            && !await productService.IsBarcodeUniqueAsync(command.Barcode, command.ProductId))
        {
            return CommandResult.Failure($"Barcode '{command.Barcode.Trim()}' is already assigned to another product.");
        }

        product.Name = command.Name;
        product.SalePrice = command.SalePrice;
        product.CostPrice = command.CostPrice;
        product.Quantity = command.Quantity;
        product.TaxProfileId = command.TaxProfileId;
        product.BrandId = command.BrandId;
        product.HSNCode = string.IsNullOrWhiteSpace(command.HSNCode) ? null : command.HSNCode.Trim();
        product.Barcode = string.IsNullOrWhiteSpace(command.Barcode) ? null : command.Barcode.Trim();
        product.UOM = string.IsNullOrWhiteSpace(command.UOM) ? "pcs" : command.UOM.Trim();
        product.MinStockLevel = command.MinStockLevel;
        product.MaxStockLevel = command.MaxStockLevel;
        product.IsActive = command.IsActive;
        product.IsTaxInclusive = command.IsTaxInclusive;
        product.Color = string.IsNullOrWhiteSpace(command.Color) ? null : command.Color.Trim();
        product.RowVersion = command.RowVersion;

        await productService.UpdateAsync(product);
        return CommandResult.Success();
    }
}
