using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public sealed record CompleteFirstSetupCommand(
    string FirmName,
    string Address,
    string Phone,
    string Email,
    string GSTIN,
    string CurrencyCode,
    string AdminPin,
    string ManagerPin,
    string UserPin,
    string MasterPin) : ICommandRequest<Unit>;
