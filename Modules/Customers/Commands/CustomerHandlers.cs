using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Customers.Services;

namespace StoreAssistantPro.Modules.Customers.Commands;

public class SaveCustomerHandler(ICustomerService customerService)
    : BaseCommandHandler<SaveCustomerCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(SaveCustomerCommand command)
    {
        var customer = command.Customer;
        if (customer.Id == 0)
            await customerService.CreateAsync(customer);
        else
            await customerService.UpdateAsync(customer);
        return CommandResult.Success();
    }
}

public class DeleteCustomerHandler(ICustomerService customerService)
    : BaseCommandHandler<DeleteCustomerCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(DeleteCustomerCommand command)
    {
        await customerService.DeleteAsync(command.CustomerId);
        return CommandResult.Success();
    }
}
