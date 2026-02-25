using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Brands.Services;

namespace StoreAssistantPro.Modules.Brands.Commands;

public class UpdateBrandHandler(IBrandService brandService)
    : BaseCommandHandler<UpdateBrandCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(UpdateBrandCommand command)
    {
        var brand = new Brand
        {
            Id = command.BrandId,
            Name = command.Name.Trim(),
            IsActive = command.IsActive,
            RowVersion = command.RowVersion
        };

        await brandService.UpdateAsync(brand);
        return CommandResult.Success();
    }
}
