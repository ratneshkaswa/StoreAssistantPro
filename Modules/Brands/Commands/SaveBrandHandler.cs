using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Brands.Services;

namespace StoreAssistantPro.Modules.Brands.Commands;

public class SaveBrandHandler(IBrandService brandService)
    : BaseCommandHandler<SaveBrandCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(SaveBrandCommand command)
    {
        var brand = new Brand
        {
            Name = command.Name.Trim(),
            IsActive = command.IsActive
        };

        await brandService.AddAsync(brand);
        return CommandResult.Success();
    }
}
