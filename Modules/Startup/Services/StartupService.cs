using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Modules.Startup.Services;

public class StartupService(
    IDbContextFactory<AppDbContext> contextFactory,
    IAppStateService appState,
    IConfiguration configuration,
    IFeatureToggleService featureToggle,
    ILogger<StartupService> logger) : IStartupService
{
    public async Task MigrateDatabaseAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // 1. Verify connectivity before attempting migration
        if (!await context.Database.CanConnectAsync())
            throw new InvalidOperationException(
                "Cannot connect to the database. Verify the connection string in appsettings.json.");

        // 2. Log pending migrations before applying
        var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("Database is up to date — no pending migrations");
            return;
        }

        logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
            pending.Count, string.Join(", ", pending));

        // 3. Apply with timeout protection
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
        await context.Database.MigrateAsync();

        logger.LogInformation("All migrations applied successfully");
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
