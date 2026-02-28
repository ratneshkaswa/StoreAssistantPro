using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Staff.Commands;

public sealed record SaveStaffCommand(Models.Staff Staff) : ICommandRequest<Unit>;
public sealed record DeleteStaffCommand(int StaffId) : ICommandRequest<Unit>;
