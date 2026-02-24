namespace StoreAssistantPro.Modules.Authentication.Services;

public interface ISetupService
{
    Task InitializeAppAsync(string firmName, string address, string phone, string adminPin, string managerPin, string userPin, string masterPin);
}
