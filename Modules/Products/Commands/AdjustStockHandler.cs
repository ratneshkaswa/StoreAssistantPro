using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Products.Commands;

public class AdjustStockHandler(IProductService productService, ILogger<AdjustStockHandler> logger)
    : BaseCommandHandler<AdjustStockCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(AdjustStockCommand command)
    {
        var product = await productService.GetByIdAsync(command.ProductId);
        if (product is null)
            return CommandResult.Failure("Product not found.");

        var newQty = product.Quantity + command.AdjustmentQty;
        if (newQty < 0)
            return CommandResult.Failure($"Adjustment would result in negative stock ({newQty}). Current stock: {product.Quantity}.");

        product.Quantity = newQty;
        product.RowVersion = command.RowVersion;

        logger.LogInformation(
            "Stock adjustment: Product #{Id} '{Name}' adjusted by {Adj} (was {Old}, now {New}). Reason: {Reason}",
            product.Id, product.Name, command.AdjustmentQty,
            product.Quantity - command.AdjustmentQty, newQty,
            command.Reason ?? "(none)");

        await productService.UpdateAsync(product);
        return CommandResult.Success();
    }
}
