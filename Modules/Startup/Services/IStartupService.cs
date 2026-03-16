namespace StoreAssistantPro.Modules.Startup.Services;

public interface IStartupService
{
    Task MigrateDatabaseAsync(CancellationToken ct = default);
    Task<bool> IsAppInitializedAsync(CancellationToken ct = default);
    Task AutoInitializeIfNeededAsync(CancellationToken ct = default);
    Task LoadFirmInfoAsync(CancellationToken ct = default);
    void LoadFeatureFlags();
    Task EnsureFinancialYearAsync(CancellationToken ct = default);
}
