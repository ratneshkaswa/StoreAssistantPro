using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public sealed record CompleteFirstSetupCommand(
    string FirmName,
    string AdminPin,
    string ManagerPin,
    string UserPin,
    string MasterPin) : ICommand;
