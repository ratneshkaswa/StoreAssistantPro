using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Brands.Services;

namespace StoreAssistantPro.Modules.Brands.Commands;

public class DeleteBrandHandler(IBrandService brandService)
    : BaseCommandHandler<DeleteBrandCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(DeleteBrandCommand command)
    {
        await brandService.DeleteAsync(command.BrandId, command.RowVersion);
        return CommandResult.Success();
    }
}
