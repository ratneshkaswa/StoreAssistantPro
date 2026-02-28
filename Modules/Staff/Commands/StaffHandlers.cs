using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Staff.Services;

namespace StoreAssistantPro.Modules.Staff.Commands;

public class SaveStaffHandler(IStaffService staffService)
    : BaseCommandHandler<SaveStaffCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(SaveStaffCommand command)
    {
        var staff = command.Staff;
        if (staff.Id == 0)
            await staffService.CreateAsync(staff);
        else
            await staffService.UpdateAsync(staff);
        return CommandResult.Success();
    }
}

public class DeleteStaffHandler(IStaffService staffService)
    : BaseCommandHandler<DeleteStaffCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(DeleteStaffCommand command)
    {
        await staffService.DeleteAsync(command.StaffId);
        return CommandResult.Success();
    }
}
