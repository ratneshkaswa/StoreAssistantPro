using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public sealed record CompleteFirstSetupCommand(
    string FirmName,
    string Address,
    string State,
    string Pincode,
    string Phone,
    string Email,
    string GSTIN,
    string PAN,
    string CurrencyCode,
    string CurrencySymbol,
    int FinancialYearStartMonth,
    int FinancialYearEndMonth,
    string DateFormat,
    string AdminPin,
    string ManagerPin,
    string UserPin,
    string MasterPin) : ICommandRequest<Unit>;
