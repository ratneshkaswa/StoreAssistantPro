using StoreAssistantPro.Modules.Authentication.Commands;

namespace StoreAssistantPro.Modules.Authentication.Services;

public interface ISetupService
{
    Task InitializeAppAsync(
        string firmName, string address, string state, string pincode,
        string phone, string email, string gstin, string pan,
        string currencyCode, string currencySymbol,
        int financialYearStartMonth, int financialYearEndMonth,
        string dateFormat,
        string adminPin, string managerPin,
        string userPin, string masterPin,
        SetupBusinessOptions businessOptions,
        CancellationToken ct = default);
}
