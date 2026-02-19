using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Modules.Startup.Services;

public class StartupService(
    IDbContextFactory<AppDbContext> contextFactory,
    IAppStateService appState,
    IConfiguration configuration,
    IFeatureToggleService featureToggle) : IStartupService
{
    public async Task MigrateDatabaseAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }

    public async Task<bool> IsAppInitializedAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var config = await context.AppConfigs.FirstOrDefaultAsync();
        return config?.IsInitialized == true;
    }

    public async Task LoadFirmInfoAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var config = await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync();
        appState.SetFirmInfo(config?.FirmName ?? string.Empty);
    }

    public void LoadFeatureFlags()
    {
        var section = configuration.GetSection("Features");
        var flags = section.GetChildren()
            .ToDictionary(
                c => c.Key,
                c => bool.TryParse(c.Value, out var v) && v,
                StringComparer.OrdinalIgnoreCase);

        featureToggle.Load(flags);
    }
}
