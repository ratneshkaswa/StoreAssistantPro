using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Products.Commands;

public class BulkDeleteProductsHandler(IProductService productService)
    : ICommandRequestHandler<BulkDeleteProductsCommand, BulkDeleteProductsResult>
{
    public async Task<CommandResult<BulkDeleteProductsResult>> HandleAsync(
        BulkDeleteProductsCommand command, CancellationToken ct = default)
    {
        try
        {
            var (deleted, failedNames) = await productService.DeleteRangeAsync(command.Products, ct)
                .ConfigureAwait(false);

            var result = new BulkDeleteProductsResult(deleted, failedNames.Count, failedNames);

            if (deleted == 0 && failedNames.Count > 0)
                return CommandResult<BulkDeleteProductsResult>.Failure(
                    "Could not delete any products. They may have been modified by another user.");

            return CommandResult<BulkDeleteProductsResult>.Success(result);
        }
        catch (Exception ex)
        {
            return CommandResult<BulkDeleteProductsResult>.Failure(ex.Message);
        }
    }
}
