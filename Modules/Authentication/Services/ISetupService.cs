namespace StoreAssistantPro.Modules.Authentication.Services;

public interface ISetupService
{
    Task InitializeAppAsync(string firmName, string adminPin, string managerPin, string userPin, string masterPin);
}
