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

        product.Name = command.Name;
        product.SalePrice = command.SalePrice;
        product.Quantity = command.Quantity;
        product.TaxProfileId = command.TaxProfileId;
        product.RowVersion = command.RowVersion;

        await productService.UpdateAsync(product);
        return CommandResult.Success();
    }
}
