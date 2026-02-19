namespace StoreAssistantPro.Modules.Startup.Services;

public interface IStartupService
{
    Task MigrateDatabaseAsync();
    Task<bool> IsAppInitializedAsync();
    Task LoadFirmInfoAsync();
    void LoadFeatureFlags();
}
