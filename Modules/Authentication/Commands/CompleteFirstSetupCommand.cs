using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public sealed record CompleteFirstSetupCommand(
    string FirmName,
    string Address,
    string Phone,
    string AdminPin,
    string ManagerPin,
    string UserPin,
    string MasterPin) : ICommandRequest<Unit>;
