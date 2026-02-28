using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Products.Commands;

public class BulkPriceUpdateHandler(IProductService productService)
    : ICommandRequestHandler<BulkPriceUpdateCommand, BulkPriceUpdateResult>
{
    public async Task<CommandResult<BulkPriceUpdateResult>> HandleAsync(
        BulkPriceUpdateCommand command, CancellationToken ct = default)
    {
        try
        {
            var updated = await productService.BulkUpdatePricesAsync(
                command.CategoryId, command.Percentage, ct).ConfigureAwait(false);
            return CommandResult<BulkPriceUpdateResult>.Success(new BulkPriceUpdateResult(updated));
        }
        catch (Exception ex)
        {
            return CommandResult<BulkPriceUpdateResult>.Failure($"Bulk price update failed: {ex.Message}");
        }
    }
}
