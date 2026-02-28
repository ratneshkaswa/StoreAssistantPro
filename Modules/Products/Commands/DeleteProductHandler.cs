using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Products.Commands;

public class DeleteProductHandler(IProductService productService)
    : BaseCommandHandler<DeleteProductCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(DeleteProductCommand command)
    {
        await productService.DeleteAsync(command.ProductId, command.RowVersion);
        return CommandResult.Success();
    }
}
